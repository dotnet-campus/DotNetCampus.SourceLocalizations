using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DotNetCampus.Localizations.Assets.Templates;
using DotNetCampus.Localizations.Generators.ModelProviding;
using DotNetCampus.Localizations.IO;
using DotNetCampus.Localizations.Utils.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using static DotNetCampus.Localizations.Generators.ModelProviding.IetfLanguageTagExtensions;

namespace DotNetCampus.Localizations.Generators;

/// <summary>
/// 为标记了 <see cref="LocalizedConfigurationAttribute"/> 的 partial class 生成主类分部实现。
/// </summary>
/// <remarks>
/// <para>输出文件：<c>{TypeName}.g.cs</c></para>
/// <para>触发条件：始终。</para>
/// <para>
/// 主类提供 <c>Default</c>/<c>Current</c>/<c>SetCurrent</c>/<c>Create</c> 等入口。
/// 根据 <see cref="GenerationMode"/> 决定工厂方法实现：
/// Dictionary 模式创建 Provider；Compiled 模式返回编译实现的单例。
/// 根据 <see cref="NotificationMode"/> 决定 <c>_current</c> 类型和 <c>SetCurrent</c> 行为。
/// </para>
/// </remarks>
[Generator]
public class LocalizationTypeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptionsProvider = context.AnalyzerConfigOptionsProvider;
        var localizationFilesProvider = context.SelectLocalizationFileModels();
        var localizationTypeProvider = context.SyntaxProvider.SelectGeneratingModels();
        context.RegisterSourceOutput(
            localizationTypeProvider.Combine(globalOptionsProvider).Combine(localizationFilesProvider.Collect()),
            Execute);
    }

    private void Execute(
        SourceProductionContext context,
        ((LocalizationGeneratingModel Left, AnalyzerConfigOptionsProvider Right) Left, ImmutableArray<LocalizationFileModel> Right) values)
    {
        var ((model, options), localizationFiles) = values;

        var isIncludedByPackageReference = options.GlobalOptions.GetBoolean("LocalizationIsIncludedByPackageReference");
        var supportsNonIetfLanguageTag = options.GlobalOptions.GetBoolean("LocalizationSupportsNonIetfLanguageTag");

        if (!isIncludedByPackageReference)
        {
            return;
        }

        var allLocalizationModels = localizationFiles.GroupByIetfLanguageTag(supportsNonIetfLanguageTag)
            .ToImmutableSortedDictionary(x => x.IetfLanguageTag, x => x.Models);

        string code;
        if (model.GenerationMode == GenerationMode.Dictionary)
        {
            code = GenerateDictionaryMainClass(model, allLocalizationModels);
        }
        else
        {
            code = GenerateCompiledMainClass(model, allLocalizationModels);
        }

        context.AddSource($"{model.TypeName}.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private string GenerateDictionaryMainClass(
        LocalizationGeneratingModel model,
        ImmutableSortedDictionary<string, IReadOnlyList<LocalizationFileModel>> allLocalizationModels)
    {
        var supportsNotifyChanged = model.NotificationMode is not NotificationMode.InitOnly;
        var localizationFile = supportsNotifyChanged
            ? EmbeddedSourceFile.Get<NotifiableLocalization>()
            : EmbeddedSourceFile.Get<ImmutableLocalization>();
        var originalText = ReplaceNamespaceAndTypeName(localizationFile.Content, model.Namespace, model.TypeName);
        var localizedValuesTypeName = supportsNotifyChanged ? nameof(NotifiableLocalizedValues) : nameof(ImmutableLocalizedValues);
        var defaultCode = originalText
            .Replace("DEFAULT_IETF_LANGUAGE_TAG", model.DefaultLanguage.ToLowerInvariant())
            .Replace("\"CURRENT_IETF_LANGUAGE_TAG\"", model.CurrentLanguage is null
                ? "global::System.Globalization.CultureInfo.CurrentUICulture.Name"
                : $"\"{model.CurrentLanguage.ToLowerInvariant()}\"")
            .FlagReplace(GenerateCreateLocalizedStringProviderCore(model.DefaultLanguage, allLocalizationModels))
            .Flag2Replace(GenerateIetfLanguageTagList(allLocalizationModels.Keys))
            .Replace("ILocalizedValues", $"global::{GeneratorInfo.RootNamespace}.ILocalizedValues")
            .Replace("PlaceholderLocalizedValues", $" global::{GeneratorInfo.RootNamespace}.{localizedValuesTypeName}");
        if (supportsNotifyChanged)
        {
            defaultCode = defaultCode.Replace(
                "ILocalizedStringProvider Wrap(ILocalizedStringProvider rawProvider) => rawProvider",
                $"global::{GeneratorInfo.RootNamespace}.MutableLocalizedStringProvider Wrap(ILocalizedStringProvider rawProvider) => new global::{GeneratorInfo.RootNamespace}.MutableLocalizedStringProvider{{ Provider = rawProvider}}");
            defaultCode = defaultCode
                .Replace("public static async global::System.Threading.Tasks.ValueTask SetCurrent", "public static void SetCurrent")
                .Replace("await _current.SetProvider(CreateLocalizedStringProvider(languageTag));", "_current.SetProvider(CreateLocalizedStringProvider(languageTag));");
        }
        return defaultCode;
    }

    private string GenerateCompiledMainClass(
        LocalizationGeneratingModel model,
        ImmutableSortedDictionary<string, IReadOnlyList<LocalizationFileModel>> allLocalizationModels)
    {
        // 任务 5 实现
        return $"// TODO: Compiled main class for {model.TypeName}";
    }

    private string GenerateIetfLanguageTagList(IEnumerable<string> languageTags) => $"""
{string.Join("\n", languageTags.Select(x => $"        \"{x}\","))}
""";

    private string GenerateCreateLocalizedStringProviderCore(
        string defaultIetfTag,
        IReadOnlyDictionary<string, IReadOnlyList<LocalizationFileModel>> models) => $"""
{string.Join("\n", models.Select(x => ConvertModelToProviderPatternMatch(defaultIetfTag, x.Key)))}
""";

    private string ConvertModelToProviderPatternMatch(string defaultIetfTag, string ietfTag)
    {
        var tagIdentifier = IetfLanguageTagToIdentifier(ietfTag);
        var defaultProvider = ietfTag == defaultIetfTag
            ? "null"
            : "_default.LocalizedStringProvider";
        return $"""
            "{ietfTag.ToLowerInvariant()}" => new global::{GeneratorInfo.RootNamespace}.{nameof(LocalizedStringProvider)}_{tagIdentifier}({defaultProvider}),
""";
    }

    private static string ReplaceNamespaceAndTypeName(string sourceText, string rootNamespace, string typeName)
    {
        return sourceText
            .Replace("namespace DotNetCampus.Localizations.Assets.Templates;", $"namespace {rootNamespace};")
            .Replace("partial class ImmutableLocalization", $"partial class {typeName}")
            .Replace("partial class NotifiableLocalization", $"partial class {typeName}")
            .Replace("static ImmutableLocalization()", $"static {typeName}()")
            .Replace("static NotifiableLocalization()", $"static {typeName}()");
    }
}
