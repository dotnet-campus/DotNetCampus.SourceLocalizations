using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DotNetCampus.Localizations.Generators.Builders;
using DotNetCampus.Localizations.Generators.CodeTransforming;
using DotNetCampus.Localizations.Generators.ModelProviding;
using DotNetCampus.Localizations.Utils.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using static DotNetCampus.Localizations.Generators.ModelProviding.IetfLanguageTagExtensions;

namespace DotNetCampus.Localizations.Generators;

/// <summary>
/// 为 Compiled 模式生成每种语言的实现类（单类 + 显式接口实现 + 字面量 + 单例）。
/// </summary>
/// <remarks>
/// <para>输出文件：每种语言一个 <c>LocalizedValues_{Tag}.g.cs</c>；INPC 时额外一个 <c>NotifiableLocalizedValues.g.cs</c>。</para>
/// <para>触发条件：<see cref="GenerationMode.Compiled"/>。</para>
/// <para>
/// 每种语言只有一个类，通过显式接口实现所有层级接口。
/// 导航属性返回 <c>this</c>（零分配），叶子属性返回 <c>new LocalizedString("key", "literal")</c>。
/// INPC 时额外生成 <c>NotifiableLocalizedValues</c> 单类包装（持有 <c>_inner</c> + <c>SetInner</c> + raise 所有叶子属性名）。
/// </para>
/// </remarks>
[Generator]
public class CompiledValuesGenerator : IIncrementalGenerator
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
        var ((options, localizationFiles), localizationTypes) = values;
        var localizationType = localizationTypes.FirstOrDefault();

        if (localizationType == default)
        {
            return;
        }

        if (localizationType.GenerationMode != GenerationMode.Compiled)
        {
            return;
        }

        if (localizationType.NotificationMode == NotificationMode.LocalizationItemPropertyChanged)
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

        if (!allTags.Contains(localizationType.DefaultLanguage))
        {
            return;
        }

        var referenceLanguageTag = localizationType.DefaultLanguage;

        // 使用 reference 语言的 tree 结构（所有语言共享相同的 key 结构）
        var referenceTransformer = new LocalizationCodeTransformer(allLocalizationModels[referenceLanguageTag]);

        // 为每种语言生成一个编译实现类
        foreach (var pair in allLocalizationModels)
        {
            var (ietfLanguageTag, group) = (pair.Key, pair.Value);
            var transformer = new LocalizationCodeTransformer(group);
            var code = transformer.ToCompiledValuesCodeText(localizationType, ietfLanguageTag, referenceTransformer);
            context.AddSource($"LocalizedValues_{IetfLanguageTagToIdentifier(ietfLanguageTag)}.g.cs", SourceText.From(code, Encoding.UTF8));
        }

        // INPC 时额外生成 NotifiableLocalizedValues 包装类
        if (localizationType.NotificationMode != NotificationMode.InitOnly)
        {
            var code = referenceTransformer.ToCompiledNotifiableValuesCodeText(localizationType);
            context.AddSource("NotifiableLocalizedValues.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}
