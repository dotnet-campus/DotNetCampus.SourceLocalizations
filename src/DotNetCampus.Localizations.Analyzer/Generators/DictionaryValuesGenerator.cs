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
/// 为 Dictionary 模式生成实现类（多类树方案，WPF 兼容）。
/// </summary>
/// <remarks>
/// <para>输出文件：<c>LocalizedValues.immutable.g.cs</c>（始终）和 <c>LocalizedValues.notifiable.g.cs</c>（当 <see cref="NotificationMode"/> 非 InitOnly 时）。</para>
/// <para>触发条件：<see cref="GenerationMode.Dictionary"/>。</para>
/// <para>
/// 每个非叶节点对应一个实现类，公开属性实现接口（WPF 绑定路径兼容）。
/// Notifiable 系列额外实现 <c>INotifyPropertyChanged</c>，每节点独立 raise 自己的叶子属性，
/// 并通过递归 <c>SetProvider()</c> 在切换语言时逐级通知。
/// </para>
/// </remarks>
[Generator]
public class DictionaryValuesGenerator : IIncrementalGenerator
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

        if (localizationType.GenerationMode != GenerationMode.Dictionary)
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

        var immutableCode = transformer.ToDictionaryImmutableValuesCodeText(localizationType);
        context.AddSource("LocalizedValues.immutable.g.cs", SourceText.From(immutableCode, Encoding.UTF8));

        if (localizationType.NotificationMode != NotificationMode.InitOnly)
        {
            var notifiableCode = transformer.ToDictionaryNotifiableValuesCodeText(localizationType);
            context.AddSource("LocalizedValues.notifiable.g.cs", SourceText.From(notifiableCode, Encoding.UTF8));
        }
    }
}
