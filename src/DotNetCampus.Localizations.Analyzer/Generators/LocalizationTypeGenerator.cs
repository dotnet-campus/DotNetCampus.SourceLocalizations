using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DotNetCampus.Localizations.Assets.Templates;
using DotNetCampus.Localizations.Generators.Builders;
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

        if (!allLocalizationModels.ContainsKey(model.DefaultLanguage))
        {
            return;
        }

        if (model.GenerationMode == GenerationMode.Compiled
            && model.NotificationMode == NotificationMode.LocalizationItemPropertyChanged)
        {
            context.ReportInvalidConfigurationCombination();
            return;
        }

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
        var isNestedSource = model.DependencyMode == DependencyMode.NestedSource;
        var supportsNotifyChanged = model.NotificationMode is not NotificationMode.InitOnly;
        var localizationFile = supportsNotifyChanged
            ? EmbeddedSourceFile.Get<NotifiableLocalization>()
            : EmbeddedSourceFile.Get<ImmutableLocalization>();
        var originalText = ReplaceNamespaceAndTypeName(localizationFile.Content, model.Namespace, model.TypeName);
        var localizedValuesTypeName = supportsNotifyChanged ? nameof(NotifiableLocalizedValues) : nameof(ImmutableLocalizedValues);
        var typePrefix = isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.";
        var defaultCode = originalText
            .Replace("DEFAULT_IETF_LANGUAGE_TAG", model.DefaultLanguage.ToLowerInvariant())
            .Replace("\"CURRENT_IETF_LANGUAGE_TAG\"", model.CurrentLanguage is null
                ? "global::System.Globalization.CultureInfo.CurrentUICulture.Name"
                : $"\"{model.CurrentLanguage.ToLowerInvariant()}\"")
            .FlagReplace(GenerateCreateLocalizedStringProviderCore(model.DefaultLanguage, allLocalizationModels, typePrefix))
            .Flag2Replace(GenerateIetfLanguageTagList(allLocalizationModels.Keys))
            .Replace("ILocalizedValues", $"{typePrefix}ILocalizedValues")
            .Replace("PlaceholderLocalizedValues", $" {typePrefix}{localizedValuesTypeName}");
        if (supportsNotifyChanged)
        {
            defaultCode = defaultCode.Replace(
                "ILocalizedStringProvider Wrap(ILocalizedStringProvider rawProvider) => rawProvider",
                $"{typePrefix}MutableLocalizedStringProvider Wrap(ILocalizedStringProvider rawProvider) => new {typePrefix}MutableLocalizedStringProvider{{ Provider = rawProvider}}");
            defaultCode = defaultCode
                .Replace("public static async global::System.Threading.Tasks.ValueTask SetCurrent", "public static void SetCurrent")
                .Replace("await _current.SetProvider(CreateLocalizedStringProvider(languageTag));", "_current.SetProvider(CreateLocalizedStringProvider(languageTag));");
        }
        if (isNestedSource)
        {
            defaultCode = defaultCode.Replace("using global::DotNetCampus.Localizations;\n", "");
        }
        return defaultCode;
    }

    private string GenerateCompiledMainClass(
        LocalizationGeneratingModel model,
        ImmutableSortedDictionary<string, IReadOnlyList<LocalizationFileModel>> allLocalizationModels)
    {
        var isNestedSource = model.DependencyMode == DependencyMode.NestedSource;
        var supportsNotifyChanged = model.NotificationMode is not NotificationMode.InitOnly;
        using var builder = new SourceTextBuilder(model.Namespace);

        var allTags = allLocalizationModels.Keys.ToList();
        var currentLanguageExpr = model.CurrentLanguage is null
            ? "global::System.Globalization.CultureInfo.CurrentUICulture.Name"
            : $"\"{model.CurrentLanguage.ToLowerInvariant()}\"";

        var tagListLiteral = string.Join("\n", allTags.Select(t => $"    \"{t}\","));
        var switchArms = string.Join("\n", allTags.Select(t =>
            $"    \"{t.ToLowerInvariant()}\" => {(isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.")}LocalizedValues_{IetfLanguageTagToIdentifier(t)}.Instance,"));
        var fallbackExpr = isNestedSource
            ? "LocalizationFallbackHelper.FindBestMatch(languageTag, SupportedLanguageTags)"
            : "global::DotNetCampus.Localizations.Helpers.LocalizationHelper.MatchWithFallback(languageTag, SupportedLanguageTags)";
        var defaultArm = $"    _ => {fallbackExpr} is {{ }} fallback ? Create(fallback) : _default,";
        var switchBody = $"{switchArms}\n{defaultArm}";

        var defaultTagIdentifier = IetfLanguageTagToIdentifier(model.DefaultLanguage);
        var defaultExpr = $"{(isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.")}LocalizedValues_{defaultTagIdentifier}.Instance";
        var interfacePrefix = isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.";

        if (supportsNotifyChanged)
        {
            builder.AddTypeDeclaration($"partial class {model.TypeName}", t => t
                .AddRawMembers(
                    $"private static readonly {interfacePrefix}ILocalizedValues _default = {defaultExpr};",
                    $$"""
                    private static readonly {{interfacePrefix}}NotifiableLocalizedValues _current;

                    static {{model.TypeName}}()
                    {
                        var initialLang = Create({{currentLanguageExpr}});
                        _current = new {{interfacePrefix}}NotifiableLocalizedValues(initialLang);
                    }
                    """,
                    $$"""
                    public static global::System.Collections.Generic.IReadOnlyList<string> SupportedLanguageTags { get; } =
                    [
                    {{tagListLiteral}}
                    ];
                    """,
                    $"public static {interfacePrefix}ILocalizedValues Default => _default;",
                    $"public static {interfacePrefix}NotifiableLocalizedValues Current => _current;",
                    $$"""
                    public static void SetCurrent(string languageTag)
                    {
                        var newInner = Create(languageTag);
                        _current.SetInner(newInner);
                    }
                    """,
                    $$"""
                    public static {{interfacePrefix}}ILocalizedValues Create(string languageTag)
                    {
                        return languageTag.ToLowerInvariant() switch
                        {
                    {{switchBody}}
                        };
                    }
                    """)
            );
        }
        else
        {
            builder.AddTypeDeclaration($"partial class {model.TypeName}", t => t
                .AddRawMembers(
                    $"private static readonly {interfacePrefix}ILocalizedValues _default = {defaultExpr};",
                    $$"""
                    private static {{interfacePrefix}}ILocalizedValues _current;

                    static {{model.TypeName}}()
                    {
                        _current = Create({{currentLanguageExpr}});
                    }
                    """,
                    $$"""
                    public static global::System.Collections.Generic.IReadOnlyList<string> SupportedLanguageTags { get; } =
                    [
                    {{tagListLiteral}}
                    ];
                    """,
                    $"public static {interfacePrefix}ILocalizedValues Default => _default;",
                    $"public static {interfacePrefix}ILocalizedValues Current => _current;",
                    $$"""
                    public static void SetCurrent(string languageTag)
                    {
                        _current = Create(languageTag);
                    }
                    """,
                    $$"""
                    public static {{interfacePrefix}}ILocalizedValues Create(string languageTag)
                    {
                        return languageTag.ToLowerInvariant() switch
                        {
                    {{switchBody}}
                        };
                    }
                    """)
            );
        }

        return builder.ToString();
    }

    private string GenerateIetfLanguageTagList(IEnumerable<string> languageTags) => $"""
{string.Join("\n", languageTags.Select(x => $"        \"{x}\","))}
""";

    private string GenerateCreateLocalizedStringProviderCore(
        string defaultIetfTag,
        IReadOnlyDictionary<string, IReadOnlyList<LocalizationFileModel>> models,
        string typePrefix) => $"""
{string.Join("\n", models.Select(x => ConvertModelToProviderPatternMatch(defaultIetfTag, x.Key, typePrefix)))}
""";

    private string ConvertModelToProviderPatternMatch(string defaultIetfTag, string ietfTag, string typePrefix)
    {
        var tagIdentifier = IetfLanguageTagToIdentifier(ietfTag);
        var defaultProvider = ietfTag == defaultIetfTag
            ? "null"
            : "_default.LocalizedStringProvider";
        return $"""
            "{ietfTag.ToLowerInvariant()}" => new {typePrefix}{nameof(LocalizedStringProvider)}_{tagIdentifier}({defaultProvider}),
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
