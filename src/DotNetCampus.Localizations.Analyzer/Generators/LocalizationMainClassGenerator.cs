using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DotNetCampus.Localizations.Generators.Builders;
using DotNetCampus.Localizations.Generators.ModelProviding;
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
public class LocalizationMainClassGenerator : IIncrementalGenerator
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
            .ToImmutableSortedDictionary(x => x.IetfLanguageTag, x => x.Models, StringComparer.OrdinalIgnoreCase);

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
        using var builder = new SourceTextBuilder(model.Namespace);

        var allTags = allLocalizationModels.Keys.ToList();
        var typePrefix = isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.";
        var currentLanguageExpression = model.CurrentLanguage is null
            ? "global::System.Globalization.CultureInfo.CurrentUICulture.Name"
            : $"\"{model.CurrentLanguage.ToLowerInvariant()}\"";

        var tagListLiteral = string.Join("\n", allTags.Select(tag => $"    \"{tag}\","));
        var switchArms = string.Join("\n", allLocalizationModels.Select(pair =>
            ConvertModelToProviderPatternMatch(model.DefaultLanguage, pair.Key, typePrefix)));
        var fallbackExpression = isNestedSource
            ? "LocalizationFallbackHelper.FindBestMatch(languageTag, SupportedLanguageTags)"
            : "global::DotNetCampus.Localizations.Helpers.LocalizationHelper.MatchWithFallback(languageTag, SupportedLanguageTags)";

        if (supportsNotifyChanged)
        {
            builder.AddTypeDeclaration($"partial class {model.TypeName}", type => type
                .AddRawMembers(
                    $"private static readonly {typePrefix}ImmutableLocalizedValues _default = new {typePrefix}ImmutableLocalizedValues(CreateLocalizedStringProvider(\"{model.DefaultLanguage.ToLowerInvariant()}\"));",
                    $$"""
                    private static readonly {{typePrefix}}NotifiableLocalizedValues _current;

                    static {{model.TypeName}}()
                    {
                        _current = new {{typePrefix}}NotifiableLocalizedValues(CreateLocalizedStringProvider({{currentLanguageExpression}}));
                    }
                    """,
                    $$"""
                    /// <summary>
                    /// 获取支持的语言标签列表。
                    /// </summary>
                    public static global::System.Collections.Generic.IReadOnlyList<string> SupportedLanguageTags { get; } =
                    [
                    {{tagListLiteral}}
                    ];
                    """,
                    $"""
                    /// <summary>
                    /// 获取默认语言的本地化字符串集。
                    /// </summary>
                    public static {typePrefix}ILocalizedValues Default => _default;
                    """,
                    $"""
                    /// <summary>
                    /// 获取当前语言的本地化字符串集。切换语言时，此实例会通过属性变更通知更新绑定的 UI。
                    /// </summary>
                    public static {typePrefix}NotifiableLocalizedValues Current => _current;
                    """,
                    $$"""
                    /// <summary>
                    /// 切换当前语言。
                    /// </summary>
                    /// <param name="languageTag">要切换到的语言标签。</param>
                    public static void SetCurrent(string languageTag)
                    {
                        _current.SetProvider(CreateLocalizedStringProvider(languageTag));
                    }
                    """,
                    $"""
                    /// <summary>
                    /// 创建指定语言的本地化字符串集实例。
                    /// </summary>
                    /// <param name="languageTag">语言标签。</param>
                    /// <returns>对应语言的本地化字符串集。</returns>
                    public static {typePrefix}ILocalizedValues Create(string languageTag) => new {typePrefix}ImmutableLocalizedValues(CreateLocalizedStringProvider(languageTag));
                    """,
                    $$"""
                    private static {{typePrefix}}ILocalizedStringProvider CreateLocalizedStringProvider(string languageTag)
                    {
                        var provider = CreateLocalizedStringProviderCore(languageTag);
                        if (provider is not null)
                        {
                            return provider;
                        }
                        var fallbackTag = {{fallbackExpression}};
                        provider = fallbackTag is null ? null : CreateLocalizedStringProviderCore(fallbackTag);
                        if (provider is not null)
                        {
                            return provider;
                        }
                        return _default.LocalizedStringProvider;
                    }
                    """,
                    $$"""
                    private static {{typePrefix}}ILocalizedStringProvider? CreateLocalizedStringProviderCore(string languageTag)
                    {
                        return languageTag.ToLowerInvariant() switch
                        {
                    {{switchArms}}
                            _ => null,
                        };
                    }
                    """)
            );
        }
        else
        {
            builder.AddTypeDeclaration($"partial class {model.TypeName}", type => type
                .AddRawMembers(
                    $"private static readonly {typePrefix}ImmutableLocalizedValues _default = GetOrCreateLocalizedValues(\"{model.DefaultLanguage.ToLowerInvariant()}\");",
                    $$"""
                    private static {{typePrefix}}ImmutableLocalizedValues _current;

                    static {{model.TypeName}}()
                    {
                        _current = GetOrCreateLocalizedValues({{currentLanguageExpression}});
                    }
                    """,
                    $$"""
                    /// <summary>
                    /// 获取支持的语言标签列表。
                    /// </summary>
                    public static global::System.Collections.Generic.IReadOnlyList<string> SupportedLanguageTags { get; } =
                    [
                    {{tagListLiteral}}
                    ];
                    """,
                    $"""
                    /// <summary>
                    /// 获取默认语言的本地化字符串集。
                    /// </summary>
                    public static {typePrefix}ILocalizedValues Default => _default;
                    """,
                    $"""
                    /// <summary>
                    /// 获取当前语言的本地化字符串集。调用 <see cref="SetCurrent(string)"/> 后需重新访问此属性获取新值。
                    /// </summary>
                    public static {typePrefix}ILocalizedValues Current => _current;
                    """,
                    $$"""
                    /// <summary>
                    /// 切换当前语言。
                    /// </summary>
                    /// <param name="languageTag">要切换到的语言标签。</param>
                    public static void SetCurrent(string languageTag)
                    {
                        _current = ({{typePrefix}}ImmutableLocalizedValues)Create(languageTag);
                    }
                    """,
                    $"""
                    /// <summary>
                    /// 创建指定语言的本地化字符串集实例。
                    /// </summary>
                    /// <param name="languageTag">语言标签。</param>
                    /// <returns>对应语言的本地化字符串集。</returns>
                    public static {typePrefix}ILocalizedValues Create(string languageTag) => GetOrCreateLocalizedValues(languageTag);
                    """,
                    $$"""
                    private static {{typePrefix}}ImmutableLocalizedValues GetOrCreateLocalizedValues(string languageTag)
                    {
                        if (_default is { } @default && languageTag.Equals("{{model.DefaultLanguage.ToLowerInvariant()}}", global::System.StringComparison.OrdinalIgnoreCase))
                        {
                            return @default;
                        }
                        if (_current is { } current && languageTag.Equals(current.LocalizedStringProvider.IetfLanguageTag, global::System.StringComparison.OrdinalIgnoreCase))
                        {
                            return current;
                        }
                        return new {{typePrefix}}ImmutableLocalizedValues(GetOrCreateLocalizedStringProvider(languageTag));
                    }
                    """,
                    $$"""
                    private static {{typePrefix}}ILocalizedStringProvider GetOrCreateLocalizedStringProvider(string languageTag)
                    {
                        if (_default is { } @default && languageTag.ToLowerInvariant() == "{{model.DefaultLanguage.ToLowerInvariant()}}")
                        {
                            return @default.LocalizedStringProvider;
                        }
                        if (_current is { } current && languageTag.Equals(current.LocalizedStringProvider.IetfLanguageTag, global::System.StringComparison.OrdinalIgnoreCase))
                        {
                            return current.LocalizedStringProvider;
                        }
                        var provider = CreateLocalizedStringProviderCore(languageTag);
                        if (provider is not null)
                        {
                            return provider;
                        }
                        var fallbackTag = {{fallbackExpression}};
                        provider = fallbackTag is null ? null : CreateLocalizedStringProviderCore(fallbackTag);
                        if (provider is not null)
                        {
                            return provider;
                        }
                        return _default.LocalizedStringProvider;
                    }
                    """,
                    $$"""
                    private static {{typePrefix}}ILocalizedStringProvider? CreateLocalizedStringProviderCore(string languageTag)
                    {
                        return languageTag.ToLowerInvariant() switch
                        {
                    {{switchArms}}
                            _ => null,
                        };
                    }
                    """)
            );
        }

        return builder.ToString();
    }

    private string GenerateCompiledMainClass(
        LocalizationGeneratingModel model,
        ImmutableSortedDictionary<string, IReadOnlyList<LocalizationFileModel>> allLocalizationModels)
    {
        var isNestedSource = model.DependencyMode == DependencyMode.NestedSource;
        var supportsNotifyChanged = model.NotificationMode is not NotificationMode.InitOnly;
        using var builder = new SourceTextBuilder(model.Namespace);

        var allTags = allLocalizationModels.Keys.ToList();
        var currentLanguageExpression = model.CurrentLanguage is null
            ? "global::System.Globalization.CultureInfo.CurrentUICulture.Name"
            : $"\"{model.CurrentLanguage.ToLowerInvariant()}\"";

        var tagListLiteral = string.Join("\n", allTags.Select(t => $"    \"{t}\","));
        var switchArms = string.Join("\n", allTags.Select(t =>
            $"    \"{t.ToLowerInvariant()}\" => {(isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.")}LocalizedValues_{IetfLanguageTagToIdentifier(t)}.Instance,"));
        var fallbackExpression = isNestedSource
            ? "LocalizationFallbackHelper.FindBestMatch(languageTag, SupportedLanguageTags)"
            : "global::DotNetCampus.Localizations.Helpers.LocalizationHelper.MatchWithFallback(languageTag, SupportedLanguageTags)";
        var defaultArm = $"    _ => {fallbackExpression} is {{ }} fallback ? Create(fallback) : _default,";
        var switchBody = $"{switchArms}\n{defaultArm}";

        var defaultTagIdentifier = IetfLanguageTagToIdentifier(model.DefaultLanguage);
        var defaultExpression = $"{(isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.")}LocalizedValues_{defaultTagIdentifier}.Instance";
        var interfacePrefix = isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.";

        if (supportsNotifyChanged)
        {
            builder.AddTypeDeclaration($"partial class {model.TypeName}", t => t
                .AddRawMembers(
                    $"private static readonly {interfacePrefix}ILocalizedValues _default = {defaultExpression};",
                    $$"""
                    private static readonly {{interfacePrefix}}NotifiableLocalizedValues _current;

                    static {{model.TypeName}}()
                    {
                        var initialLang = Create({{currentLanguageExpression}});
                        _current = new {{interfacePrefix}}NotifiableLocalizedValues(initialLang);
                    }
                    """,
                    $$"""
                    /// <summary>
                    /// 获取支持的语言标签列表。
                    /// </summary>
                    public static global::System.Collections.Generic.IReadOnlyList<string> SupportedLanguageTags { get; } =
                    [
                    {{tagListLiteral}}
                    ];
                    """,
                    $"""
                    /// <summary>
                    /// 获取默认语言的本地化字符串集。
                    /// </summary>
                    public static {interfacePrefix}ILocalizedValues Default => _default;
                    """,
                    $"""
                    /// <summary>
                    /// 获取当前语言的本地化字符串集。切换语言时，此实例会通过属性变更通知更新绑定的 UI。
                    /// </summary>
                    public static {interfacePrefix}NotifiableLocalizedValues Current => _current;
                    """,
                    $$"""
                    /// <summary>
                    /// 切换当前语言。
                    /// </summary>
                    /// <param name="languageTag">要切换到的语言标签。</param>
                    public static void SetCurrent(string languageTag)
                    {
                        var newInner = Create(languageTag);
                        _current.SetInner(newInner);
                    }
                    """,
                    $$"""
                    /// <summary>
                    /// 创建指定语言的本地化字符串集实例。
                    /// </summary>
                    /// <param name="languageTag">语言标签。</param>
                    /// <returns>对应语言的本地化字符串集。</returns>
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
                    $"private static readonly {interfacePrefix}ILocalizedValues _default = {defaultExpression};",
                    $$"""
                    private static {{interfacePrefix}}ILocalizedValues _current;

                    static {{model.TypeName}}()
                    {
                        _current = Create({{currentLanguageExpression}});
                    }
                    """,
                    $$"""
                    /// <summary>
                    /// 获取支持的语言标签列表。
                    /// </summary>
                    public static global::System.Collections.Generic.IReadOnlyList<string> SupportedLanguageTags { get; } =
                    [
                    {{tagListLiteral}}
                    ];
                    """,
                    $"""
                    /// <summary>
                    /// 获取默认语言的本地化字符串集。
                    /// </summary>
                    public static {interfacePrefix}ILocalizedValues Default => _default;
                    """,
                    $"""
                    /// <summary>
                    /// 获取当前语言的本地化字符串集。调用 <see cref="SetCurrent(string)"/> 后需重新访问此属性获取新值。
                    /// </summary>
                    public static {interfacePrefix}ILocalizedValues Current => _current;
                    """,
                    $$"""
                    /// <summary>
                    /// 切换当前语言。
                    /// </summary>
                    /// <param name="languageTag">要切换到的语言标签。</param>
                    public static void SetCurrent(string languageTag)
                    {
                        _current = Create(languageTag);
                    }
                    """,
                    $$"""
                    /// <summary>
                    /// 创建指定语言的本地化字符串集实例。
                    /// </summary>
                    /// <param name="languageTag">语言标签。</param>
                    /// <returns>对应语言的本地化字符串集。</returns>
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

    private string ConvertModelToProviderPatternMatch(string defaultIetfTag, string ietfTag, string typePrefix)
    {
        var tagIdentifier = IetfLanguageTagToIdentifier(ietfTag);
        var defaultProvider = string.Equals(ietfTag, defaultIetfTag, StringComparison.OrdinalIgnoreCase)
            ? "null"
            : "_default.LocalizedStringProvider";
        return $"""
            "{ietfTag.ToLowerInvariant()}" => new {typePrefix}LocalizedStringProvider_{tagIdentifier}({defaultProvider}),
""";
    }
}
