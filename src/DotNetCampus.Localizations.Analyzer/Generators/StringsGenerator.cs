using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DotNetCampus.Localizations.Assets.Templates;
using DotNetCampus.Localizations.Generators.CodeTransforming;
using DotNetCampus.Localizations.Generators.ModelProviding;
using DotNetCampus.Localizations.Utils.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace DotNetCampus.Localizations.Generators;

/// <summary>
/// 为所有通过 LocalizationFile 指定的文件生成对应的 C# 代码。
/// </summary>
[Generator]
public class StringsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptionsProvider = context.AnalyzerConfigOptionsProvider;
        var localizationFilesProvider = context.SelectLocalizationFileModels().Collect();
        var localizationTypeProvider = context.SyntaxProvider.SelectGeneratingModels().Collect();
        context.RegisterSourceOutput(globalOptionsProvider.Combine(localizationFilesProvider).Combine(localizationTypeProvider), Execute);
    }

    private void Execute(SourceProductionContext context, ((AnalyzerConfigOptionsProvider Left, ImmutableArray<LocalizationFileModel> Right) Left, ImmutableArray<LocalizationGeneratingModel> Right) values)
    {
        var ((options, localizationFiles), localizationTypes) = values;
        var localizationType = localizationTypes.FirstOrDefault();

        if (localizationType == default)
        {
            return;
        }

        var isIncludedByPackageReference = options.GlobalOptions.GetBoolean("LocalizationIsIncludedByPackageReference");
        var supportsNonIetfLanguageTag = options.GlobalOptions.GetBoolean("LocalizationSupportsNonIetfLanguageTag");
        var allLocalizationModels = localizationFiles.GroupByIetfLanguageTag(supportsNonIetfLanguageTag)
            .ToImmutableSortedDictionary(x => x.IetfLanguageTag, x => x.Models);
        var allTags = allLocalizationModels.Keys.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        if (!isIncludedByPackageReference)
        {
            // 只有直接引用本地化库的项目才会生成本地化代码。
            return;
        }

        var referenceLanguageTag = allTags.Contains(localizationType.DefaultLanguage)
            ? localizationType.DefaultLanguage
            : allTags.FirstOrDefault() ?? null;
        if (referenceLanguageTag is null)
        {
            // 没有找到任何语言标签，无法生成代码。
            return;
        }


        Dictionary<string, LocalizationCodeTransformer> transformers = new();

        foreach (var pair in allLocalizationModels)
        {
            var (ietfLanguageTag, group) = (pair.Key, pair.Value);

            try
            {
                var transformer = new LocalizationCodeTransformer(group);
                transformers[ietfLanguageTag] = transformer;

                var code = transformer.ToProviderCodeText(localizationType.Namespace, ietfLanguageTag);
                context.AddSource($"Strings.{ietfLanguageTag}.g.cs", SourceText.From(code, Encoding.UTF8));

                if (string.Equals(ietfLanguageTag, referenceLanguageTag, StringComparison.OrdinalIgnoreCase))
                {
                    var interfaceCode = transformer.ToInterfaceCodeText(localizationType);
                    context.AddSource($"{nameof(ILocalizedValues)}.g.cs", SourceText.From(interfaceCode, Encoding.UTF8));

                    var immutableImplementationCode = transformer.ToImplementationCodeText(localizationType, false);
                    context.AddSource("LocalizedValues.immutable.g.cs", SourceText.From(immutableImplementationCode, Encoding.UTF8));

                    var notifiableImplementationCode = transformer.ToImplementationCodeText(localizationType, true);
                    context.AddSource("LocalizedValues.notifiable.g.cs", SourceText.From(notifiableImplementationCode, Encoding.UTF8));
                }
            }
            catch (Exception ex)
            {
                context.ReportUnknownError(ex.Message);
            }
        }

        if (localizationType.EnsureKeysIdentical)
        {
            Compare(context, transformers);
        }
    }

    /// <summary>
    /// 比较多个语言的 Key 是否一致。
    /// </summary>
    /// <param name="context"></param>
    /// <param name="transformers"></param>
    private static void Compare(SourceProductionContext context, Dictionary<string, LocalizationCodeTransformer> transformers)
    {
        if (transformers.Count <= 1)
        {
            return;
        }

        var langKeyCount = transformers.First().Value.LocalizationItems.Length;
        foreach (var transformer in transformers.Skip(1))
        {
            if (transformer.Value.LocalizationItems.Length != langKeyCount)
            {
                var message = string.Join(", ", transformers.Select(t => $"{t.Key}:{t.Value.LocalizationItems.Length}"));
                context.ReportLanguageKeyInconsistent($"Language keys count is not same. {message}");
                return;
            }
        }

        var keys = new HashSet<string>(transformers.First().Value.LocalizationItems.Select(i => i.Key));

        foreach (var transformer in transformers.Skip(1))
        {
            foreach (var item in transformer.Value.LocalizationItems)
            {
                if (keys.Add(item.Key))
                {
                    var message = $"\"{item.Key}\" not exist in \"{transformers.First().Key}\"";
                    context.ReportLanguageKeyInconsistent($"{message}");
                }
            }
        }
    }

}
