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
/// 为 Dictionary 模式下的每种语言生成一个 Provider 类（<c>LocalizedStringProvider_{Tag}</c>）。
/// </summary>
/// <remarks>
/// <para>输出文件：每种语言一个 <c>Strings.{tag}.g.cs</c></para>
/// <para>触发条件：<see cref="GenerationMode.Dictionary"/>。</para>
/// <para>Provider 内部是 <c>Dictionary&lt;string, string&gt;</c> + indexer + fallback，与 <see cref="NotificationMode"/> 无关。</para>
/// <para>
/// <see cref="DependencyMode.NestedSource"/> 时，Provider 类包裹在用户声明的 partial class 内部。
/// </para>
/// </remarks>
public class StringsProviderGenerator
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

        if (model.GenerationMode != GenerationMode.Dictionary)
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

        if (!allLocalizationModels.ContainsKey(model.DefaultLanguage))
        {
            return;
        }

        foreach (var pair in allLocalizationModels)
        {
            var (ietfLanguageTag, group) = (pair.Key, pair.Value);
            var transformer = new LocalizationCodeTransformer(group);
            var code = transformer.ToProviderCodeText(model, ietfLanguageTag);
            context.AddSource($"DotNetCampus.Localizations/Strings.{ietfLanguageTag}.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}
