using System;
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
}
