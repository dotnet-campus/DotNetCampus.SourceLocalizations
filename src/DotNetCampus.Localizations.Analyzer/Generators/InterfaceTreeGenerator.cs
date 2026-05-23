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
[Generator]
public class InterfaceTreeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
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
        var ((options, localizationFiles), localizationTypes) = values;
        var localizationType = localizationTypes.FirstOrDefault();

        if (localizationType == default)
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
            .ToImmutableSortedDictionary(x => x.IetfLanguageTag, x => x.Models);
        var allTags = allLocalizationModels.Keys.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        if (localizationType.EnsureKeysIdentical && allLocalizationModels.Count > 1)
        {
            CompareLanguageKeys(context, localizationType.DefaultLanguage, allLocalizationModels);
        }

        var referenceLanguageTag = allTags.Contains(localizationType.DefaultLanguage)
            ? localizationType.DefaultLanguage
            : allTags.FirstOrDefault() ?? null;
        if (referenceLanguageTag is null)
        {
            return;
        }

        var group = allLocalizationModels[referenceLanguageTag];
        var transformer = new LocalizationCodeTransformer(group);

        var code = localizationType.DependencyMode == DependencyMode.NestedSource
            ? transformer.ToNestedInterfaceCodeText(localizationType)
            : transformer.ToInterfaceCodeText(localizationType);

        context.AddSource("ILocalizedValues.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static void CompareLanguageKeys(
        SourceProductionContext context,
        string defaultTag,
        ImmutableSortedDictionary<string, IReadOnlyList<LocalizationFileModel>> allLocalizationModels)
    {
        var defaultModels = allLocalizationModels.GetValueOrDefault(defaultTag) ?? allLocalizationModels.Values.First();
        var defaultTransformer = new LocalizationCodeTransformer(defaultModels);
        var defaultKeys = new HashSet<string>(defaultTransformer.LocalizationItems.Select(i => i.Key));

        var diffs = new List<string>();

        foreach (var pair in allLocalizationModels)
        {
            if (string.Equals(pair.Key, defaultTag, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var transformer = new LocalizationCodeTransformer(pair.Value);
            var otherKeys = new HashSet<string>(transformer.LocalizationItems.Select(i => i.Key));

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
            context.ReportLanguageKeyInconsistent(message);
        }
    }
}
