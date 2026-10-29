using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DotNetCampus.Localizations.Generators.CodeTransforming;
using DotNetCampus.Localizations.Generators.ModelProviding;
using DotNetCampus.Localizations.Utils.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DotNetCampus.Localizations.Generators;

/// <summary>
/// 根据本地化 Key 树结构生成接口层级定义。
/// </summary>
/// <remarks>
/// <para>输出文件：<c>ILocalizedValues.g.cs</c></para>
/// <para>触发条件：始终（只要存在 <see cref="LocalizedConfigurationAttribute"/> 标记的类型且存在有效的语言文件）。</para>
/// <para>生成内容与 <see cref="GenerationMode"/>/<see cref="NotificationMode"/> 无关，仅由 Key 树决定。</para>
/// <para>
/// <see cref="DependencyMode.Library"/> 时生成顶层 public 接口（命名空间为 DotNetCampus.Localizations）；
/// <see cref="DependencyMode.NestedSource"/> 时生成 internal 接口并包裹在用户声明的 partial class 内部。
/// </para>
/// </remarks>
public class InterfaceTreeGenerator
{
    public void Register(IncrementalGeneratorInitializationContext context)
    {
        var globalOptionsProvider = context.AnalyzerConfigOptionsProvider;
        var localizationFilesProvider = context.SelectLocalizationFileModels().Collect();
        var localizationTypeProvider = context.SyntaxProvider.SelectGeneratingModels().Collect();
        context.RegisterSourceOutput(
            globalOptionsProvider.Combine(localizationFilesProvider).Combine(localizationTypeProvider),
            Execute);
    }

    private void Execute(
        SourceProductionContext context,
        ((AnalyzerConfigOptionsProvider Left, ImmutableArray<LocalizationFileModel> Right) Left, ImmutableArray<LocalizationGeneratingModel> Right) values)
    {
        try
        {
            ExecuteCore(context, values);
        }
        catch (Exception ex)
        {
            context.ReportUnknownError(ex.Message);
        }
    }

    private void ExecuteCore(
        SourceProductionContext context,
        ((AnalyzerConfigOptionsProvider Left, ImmutableArray<LocalizationFileModel> Right) Left, ImmutableArray<LocalizationGeneratingModel> Right) values)
    {
        var ((options, localizationFiles), models) = values;
        var model = models.FirstOrDefault();

        if (model == default)
        {
            return;
        }

        var isIncludedByPackageReference = options.GlobalOptions.GetBoolean("LocalizationIsIncludedByPackageReference");
        var supportsNonIetfLanguageTag = options.GlobalOptions.GetBoolean("LocalizationSupportsNonIetfLanguageTag");

        if (!isIncludedByPackageReference)
        {
            return;
        }

        var allLocalizationModels = localizationFiles.GroupByIetfLanguageTag(supportsNonIetfLanguageTag)
            .ToImmutableSortedDictionary(x => x.IetfLanguageTag, x => x.Models, StringComparer.OrdinalIgnoreCase);
        var allTags = allLocalizationModels.Keys.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        if (!allTags.Contains(model.DefaultLanguage))
        {
            context.ReportDefaultLanguageTagNotFound(
                model.Location,
                model.DefaultLanguage,
                string.Join(", ", allTags));
            return;
        }

        if (model.CurrentLanguage is { } currentLanguage && !allTags.Contains(currentLanguage))
        {
            context.ReportCurrentLanguageTagNotFound(
                model.Location,
                currentLanguage,
                string.Join(", ", allTags));
        }

        var transformers = ImmutableSortedDictionary.CreateBuilder<string, LocalizationCodeTransformer>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in allLocalizationModels)
        {
            if (!LocalizationCodeTransformer.TryCreate(pair.Value, model, out var value, out var diagnostic))
            {
                // 此路径始终执行，由它统一报告重复键；其余生成路径只终止，避免重复诊断。
                context.ReportDiagnostic(diagnostic!);
                return;
            }
            transformers.Add(pair.Key, value);
        }
        var allTransformers = transformers.ToImmutable();
        if (!ValidateLanguageKeyCasing(context, model, allTransformers))
        {
            return;
        }

        if (model.EnsureKeysIdentical && allTransformers.Count > 1)
        {
            CompareLanguageKeys(context, model.Location, model.DefaultLanguage, allTransformers);
        }

        var transformer = allTransformers[model.DefaultLanguage];
        var interfaceGen = new InterfaceCodeGenerator(transformer);
        var code = model.DependencyMode == DependencyMode.NestedSource
            ? interfaceGen.GenerateNested(model)
            : interfaceGen.Generate(model);

        context.AddSource($"{model.Namespace}.Localizations/ILocalizedValues.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    /// <summary>
    /// 忽略大小写时，各语言对应 Key 仍需保持相同拼写；冲突直接报告专用诊断并停止接口生成。
    /// </summary>
    /// <param name="context">用于报告诊断的生成上下文。</param>
    /// <param name="model">用户配置，提供大小写规则及诊断位置。</param>
    /// <param name="allTransformers">按语言分组且已通过同语言重复检查的转换器。</param>
    /// <returns>无需检查或所有拼写一致时返回 true；报告冲突时返回 false。</returns>
    private static bool ValidateLanguageKeyCasing(SourceProductionContext context, LocalizationGeneratingModel model,
        ImmutableSortedDictionary<string, LocalizationCodeTransformer> allTransformers)
    {
        // 大小写敏感时，大小写不同的完整 Key 是独立项，由键集合一致性选项决定是否允许缺项。
        if (model.IsKeysCaseSensitive)
        {
            return true;
        }
        var keys = new Dictionary<string, (string Key, string LanguageTag)>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in allTransformers)
        {
            foreach (var item in pair.Value.LocalizationItems)
            {
                if (keys.TryGetValue(item.Key, out var existing)
                    && !string.Equals(existing.Key, item.Key, StringComparison.Ordinal))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.DLA008_LocalizationKeyCasingConflict,
                        model.Location, existing.Key, existing.LanguageTag, item.Key, pair.Key));
                    return false;
                }
                keys[item.Key] = (item.Key, pair.Key);
            }
        }
        return true;
    }

    private static void CompareLanguageKeys(
        SourceProductionContext context,
        Location? location,
        string defaultTag,
        ImmutableSortedDictionary<string, LocalizationCodeTransformer> allTransformers)
    {
        var defaultTransformer = allTransformers[defaultTag];
        var defaultKeys = new HashSet<string>(defaultTransformer.LocalizationItems.Select(i => i.Key), defaultTransformer.KeyComparer);

        var diffs = new List<string>();

        foreach (var pair in allTransformers)
        {
            if (string.Equals(pair.Key, defaultTag, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var transformer = pair.Value;
            var otherKeys = new HashSet<string>(transformer.LocalizationItems.Select(i => i.Key), transformer.KeyComparer);

            var missing = defaultKeys.Where(k => !otherKeys.Contains(k)).ToList();
            var extra = otherKeys.Where(k => !defaultKeys.Contains(k)).ToList();

            if (missing.Count == 0 && extra.Count == 0)
            {
                continue;
            }

            var parts = new List<string> { $"{pair.Key} {transformer.LocalizationItems.Length} 项" };
            if (missing.Count > 0)
            {
                parts.Add($"缺少 {string.Join(", ", missing.Select(k => $"\"{k}\""))}");
            }
            if (extra.Count > 0)
            {
                parts.Add($"多了 {string.Join(", ", extra.Select(k => $"\"{k}\""))}");
            }

            diffs.Add(string.Join("，", parts));
        }

        if (diffs.Count > 0)
        {
            var message = $"默认（{defaultTag}）{defaultTransformer.LocalizationItems.Length} 项。但是：{string.Join("；", diffs)}。";
            context.ReportLanguageKeyInconsistent(location, message);
        }
    }
}
