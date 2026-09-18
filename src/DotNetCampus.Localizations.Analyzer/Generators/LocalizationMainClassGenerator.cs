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
public class LocalizationMainClassGenerator
{
    public void Register(IncrementalGeneratorInitializationContext context)
    {
        var globalOptionsProvider = context.AnalyzerConfigOptionsProvider;
        var localizationFilesProvider = context.SelectLocalizationFileModels();
        var localizationTypeProvider = context.SyntaxProvider.SelectGeneratingModels();
        context.RegisterSourceOutput
        (
            localizationTypeProvider.Combine(globalOptionsProvider).Combine(localizationFilesProvider.Collect()),
            Execute
        );
    }

    private void Execute
    (
        SourceProductionContext context,
        ((LocalizationGeneratingModel Left, AnalyzerConfigOptionsProvider Right) Left,
            ImmutableArray<LocalizationFileModel> Right) values
    )
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

    private void ExecuteCore
    (
        SourceProductionContext context,
        ((LocalizationGeneratingModel Left, AnalyzerConfigOptionsProvider Right) Left,
            ImmutableArray<LocalizationFileModel> Right) values
    )
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
            context.ReportInvalidConfigurationCombination(model.Location);
            return;
        }

        if (model.GenerationMode == GenerationMode.Compiled && model.SupportsAddingProviders)
        {
            context.ReportCompiledModeDoesNotSupportAddingProviders(model.Location);
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

        context.AddSource
            ($"{model.Namespace}.Localizations/{model.TypeName}.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private string GenerateDictionaryMainClass
    (
        LocalizationGeneratingModel model,
        ImmutableSortedDictionary<string, IReadOnlyList<LocalizationFileModel>> allLocalizationModels
    )
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
        var switchArms = string.Join
        (
            "\n", allLocalizationModels.Select
            (pair =>
                ConvertModelToProviderPatternMatch(model.DefaultLanguage, pair.Key, typePrefix)
            )
        );
        var fallbackExpression = isNestedSource
            ? "LocalizationFallbackHelper.FindBestMatch(languageTag, SupportedLanguageTags)"
            : "global::DotNetCampus.Localizations.Helpers.LocalizationHelper.MatchWithFallback(languageTag, SupportedLanguageTags)";
        var providerCompositionMembers = GenerateProviderCompositionMembers(model, typePrefix);
        var providerRegistryField = model.SupportsAddingProviders
            ? "private static readonly LocalizedStringProviderRegistry _localizedStringProviderRegistry;"
            : string.Empty;
        var initializeProviderRegistry = model.SupportsAddingProviders
            ? "_localizedStringProviderRegistry = new LocalizedStringProviderRegistry();"
            : string.Empty;
        var supportedLanguageTagsMember = $$"""
                                            /// <summary>
                                            /// 获取支持的语言标签列表。
                                            /// </summary>
                                            public static global::System.Collections.Generic.IReadOnlyList<string> SupportedLanguageTags { get; } =
                                            [
                                            {{tagListLiteral}}
                                            ];
                                            """;
        var defaultPropertyMember = $"""
                                     /// <summary>
                                     /// 获取默认语言的本地化字符串集。
                                     /// </summary>
                                     public static {typePrefix}IDictionaryLocalizedValues Default => _default;
                                     """;
        var providerFactoryCoreMember = $$"""
                                          private static {{typePrefix}}ILocalizedStringProvider? CreateLocalizedStringProviderCore(string languageTag)
                                          {
                                              return languageTag.ToLowerInvariant() switch
                                              {
                                          {{switchArms}}
                                                  _ => null,
                                              };
                                          }
                                          """;

        string currentFieldAndConstructorMember;
        string currentPropertyMember;
        string setCurrentMember;
        string createMember;
        string localizedValuesCacheMember;
        string providerFactoryMember;

        if (supportsNotifyChanged)
        {
            currentFieldAndConstructorMember = $$"""
                                                 private static readonly {{typePrefix}}NotifiableLocalizedValues _current;

                                                 static {{model.TypeName}}()
                                                 {
                                                     {{initializeProviderRegistry}}
                                                     _default = new {{typePrefix}}ImmutableLocalizedValues(ComposeLocalizedStringProvider(CreateLocalizedStringProvider("{{model.DefaultLanguage.ToLowerInvariant()}}")));
                                                     _current = new {{typePrefix}}NotifiableLocalizedValues(ComposeLocalizedStringProvider(CreateLocalizedStringProvider({{currentLanguageExpression}})));
                                                 }
                                                 """;
            currentPropertyMember = $"""
                                     /// <summary>
                                     /// 获取当前语言的本地化字符串集。切换语言时，此实例会通过属性变更通知更新绑定的 UI。
                                     /// </summary>
                                     public static {typePrefix}NotifiableLocalizedValues Current => _current;
                                     """;
            setCurrentMember = $$"""
                                 /// <summary>
                                 /// 切换当前语言。
                                 /// </summary>
                                 /// <param name="languageTag">要切换到的语言标签。</param>
                                 public static void SetCurrent(string languageTag)
                                 {
                                     _current.SetProvider(ComposeLocalizedStringProvider(CreateLocalizedStringProvider(languageTag)));
                                 }
                                 """;
            createMember = $"""
                            /// <summary>
                            /// 创建指定语言的本地化字符串集实例。
                            /// </summary>
                            /// <param name="languageTag">语言标签。</param>
                            /// <returns>对应语言的本地化字符串集。</returns>
                            public static {typePrefix}IDictionaryLocalizedValues Create(string languageTag) => new {typePrefix}ImmutableLocalizedValues(ComposeLocalizedStringProvider(CreateLocalizedStringProvider(languageTag)));
                            """;
            localizedValuesCacheMember = string.Empty;
            providerFactoryMember = GenerateDictionaryProviderFactoryMember
                (typePrefix, fallbackExpression, model.DefaultLanguage, useCachedValues: false);
        }
        else
        {
            currentFieldAndConstructorMember = $$"""
                                                 private static {{typePrefix}}ImmutableLocalizedValues _current;

                                                 static {{model.TypeName}}()
                                                 {
                                                     {{initializeProviderRegistry}}
                                                     _default = GetOrCreateLocalizedValues("{{model.DefaultLanguage.ToLowerInvariant()}}");
                                                     _current = GetOrCreateLocalizedValues({{currentLanguageExpression}});
                                                 }
                                                 """;
            currentPropertyMember = $"""
                                     /// <summary>
                                     /// 获取当前语言的本地化字符串集。调用 <see cref="SetCurrent(string)"/> 后需重新访问此属性获取新值。
                                     /// </summary>
                                     public static {typePrefix}IDictionaryLocalizedValues Current => _current;
                                     """;
            setCurrentMember = $$"""
                                 /// <summary>
                                 /// 切换当前语言。
                                 /// </summary>
                                 /// <param name="languageTag">要切换到的语言标签。</param>
                                 public static void SetCurrent(string languageTag)
                                 {
                                     _current = ({{typePrefix}}ImmutableLocalizedValues)Create(languageTag);
                                 }
                                 """;
            createMember = $"""
                            /// <summary>
                            /// 创建指定语言的本地化字符串集实例。
                            /// </summary>
                            /// <param name="languageTag">语言标签。</param>
                            /// <returns>对应语言的本地化字符串集。</returns>
                            public static {typePrefix}IDictionaryLocalizedValues Create(string languageTag) => GetOrCreateLocalizedValues(languageTag);
                            """;
            localizedValuesCacheMember = $$"""
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
                                               return new {{typePrefix}}ImmutableLocalizedValues(ComposeLocalizedStringProvider(GetOrCreateLocalizedStringProvider(languageTag)));
                                           }
                                           """;
            providerFactoryMember = GenerateDictionaryProviderFactoryMember
                (typePrefix, fallbackExpression, model.DefaultLanguage, useCachedValues: true);
        }

        builder.AddTypeDeclaration
        (
            $"partial class {model.TypeName}",
            type => type.AddRawMembers
            (
                providerRegistryField,
                $"private static readonly {typePrefix}ImmutableLocalizedValues _default;",
                currentFieldAndConstructorMember,
                supportedLanguageTagsMember,
                defaultPropertyMember,
                currentPropertyMember,
                setCurrentMember,
                createMember,
                localizedValuesCacheMember,
                providerFactoryMember,
                providerFactoryCoreMember,
                providerCompositionMembers
            )
        );

        return builder.ToString();
    }

    private static string GenerateDictionaryProviderFactoryMember
    (
        string typePrefix,
        string fallbackExpression,
        string defaultLanguage,
        bool useCachedValues
    )
    {
        var methodName = useCachedValues
            ? "GetOrCreateLocalizedStringProvider"
            : "CreateLocalizedStringProvider";
        var cachedProviderLookup = useCachedValues
            ? $$"""
                if (_default is { } @default && languageTag.Equals("{{defaultLanguage.ToLowerInvariant()}}", global::System.StringComparison.OrdinalIgnoreCase))
                {
                    return @default.LocalizedStringProvider;
                }
                if (_current is { } current && languageTag.Equals(current.LocalizedStringProvider.IetfLanguageTag, global::System.StringComparison.OrdinalIgnoreCase))
                {
                    return current.LocalizedStringProvider;
                }
                """
            : string.Empty;

        return $$"""
                 private static {{typePrefix}}ILocalizedStringProvider {{methodName}}(string languageTag)
                 {
                 {{cachedProviderLookup}}
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
                 """;
    }

    private static string GenerateProviderCompositionMembers(LocalizationGeneratingModel model, string typePrefix)
    {
        if (!model.SupportsAddingProviders)
        {
            return $$"""
                     private static {{typePrefix}}ILocalizedStringProvider ComposeLocalizedStringProvider({{typePrefix}}ILocalizedStringProvider provider)
                     {
                         return provider;
                     }
                     """;
        }

        var supportsNotifications = model.NotificationMode != NotificationMode.InitOnly;
        var notifyProviderChanged = supportsNotifications
            ? "_current.SetProvider(_current.LocalizedStringProvider);"
            : string.Empty;
        var subscribeProviderChanged = supportsNotifications
            ? """
              if (provider is global::System.ComponentModel.INotifyPropertyChanged changed)
              {
                  changed.PropertyChanged += OnRegisteredProviderPropertyChanged;
              }
              """
            : string.Empty;
        var unsubscribeProviderChanged = supportsNotifications
            ? """
              if (entries[index].Provider is global::System.ComponentModel.INotifyPropertyChanged changed)
              {
                  changed.PropertyChanged -= OnRegisteredProviderPropertyChanged;
              }
              """
            : string.Empty;
        var registeredProviderChangedHandler = supportsNotifications
            ? """
              private static void OnRegisteredProviderPropertyChanged(object? sender, global::System.ComponentModel.PropertyChangedEventArgs e)
              {
                  _current.SetProvider(_current.LocalizedStringProvider);
              }
              """
            : string.Empty;
        var removeProviderBody = supportsNotifications
            ? """
              var removed = _localizedStringProviderRegistry.RemoveProvider(provider);
              if (removed)
              {
                  _current.SetProvider(_current.LocalizedStringProvider);
              }
              return removed;
              """
            : "return _localizedStringProviderRegistry.RemoveProvider(provider);";

        return $$"""
                 {{registeredProviderChangedHandler}}

                 private static {{typePrefix}}ILocalizedStringProvider ComposeLocalizedStringProvider({{typePrefix}}ILocalizedStringProvider provider)
                 {
                     return _localizedStringProviderRegistry.CreateProvider(provider);
                 }

                 /// <summary>
                 /// 添加本地化字符串提供者。当前 Lang 自身 Provider 的优先级为 0；正优先级可覆盖自身文本，负优先级作为兜底。
                 /// </summary>
                 /// <param name="provider">要添加的本地化字符串提供者。</param>
                 /// <param name="priority">Provider 优先级。新增的优先级 0 Provider 在当前 Lang 自身 Provider 之后查询。</param>
                 public static void AddProvider({{typePrefix}}ILocalizedStringProvider provider, int priority = 0)
                 {
                     _localizedStringProviderRegistry.AddProvider(provider, priority);
                     {{notifyProviderChanged}}
                 }

                 /// <summary>
                 /// 移除本地化字符串提供者。
                 /// </summary>
                 public static bool RemoveProvider({{typePrefix}}ILocalizedStringProvider provider)
                 {
                     {{removeProviderBody}}
                 }

                 private sealed class LocalizedStringProviderRegistry
                 {
                     private Entry[] _entries = [];
                     private long _nextOrder;

                     public void AddProvider({{typePrefix}}ILocalizedStringProvider provider, int priority)
                     {
                         if (provider is null)
                         {
                             throw new global::System.ArgumentNullException(nameof(provider));
                         }

                         var entries = new Entry[_entries.Length + 1];
                         global::System.Array.Copy(_entries, entries, _entries.Length);
                         entries[^1] = new Entry(provider, priority, _nextOrder++);
                         global::System.Array.Sort(entries, CompareEntries);
                         _entries = entries;
                         {{subscribeProviderChanged}}
                     }

                     public bool RemoveProvider({{typePrefix}}ILocalizedStringProvider provider)
                     {
                         if (provider is null)
                         {
                             throw new global::System.ArgumentNullException(nameof(provider));
                         }

                         var entries = _entries;
                         var index = global::System.Array.FindIndex(entries, entry => global::System.Object.ReferenceEquals(entry.Provider, provider));
                         if (index < 0)
                         {
                             return false;
                         }

                         {{unsubscribeProviderChanged}}

                         var newEntries = new Entry[entries.Length - 1];
                         global::System.Array.Copy(entries, 0, newEntries, 0, index);
                         global::System.Array.Copy(entries, index + 1, newEntries, index, entries.Length - index - 1);
                         _entries = newEntries;
                         return true;
                     }

                     public {{typePrefix}}ILocalizedStringProvider CreateProvider({{typePrefix}}ILocalizedStringProvider provider)
                     {
                         return new CombinedProvider(this, provider);
                     }

                     private static int CompareEntries(Entry left, Entry right)
                     {
                         var priorityComparison = right.Priority.CompareTo(left.Priority);
                         return priorityComparison != 0 ? priorityComparison : left.Order.CompareTo(right.Order);
                     }

                     private sealed record Entry({{typePrefix}}ILocalizedStringProvider Provider, int Priority, long Order);

                     private sealed class CombinedProvider(
                         LocalizedStringProviderRegistry registry,
                         {{typePrefix}}ILocalizedStringProvider provider) : {{typePrefix}}ILocalizedStringProvider
                     {
                         public string IetfLanguageTag
                         {
                             get
                             {
                                 var entries = registry._entries;
                                 return entries.Length > 0 && entries[0].Priority > 0
                                     ? entries[0].Provider.IetfLanguageTag
                                     : provider.IetfLanguageTag;
                             }
                         }

                         public string this[string key]
                         {
                             get
                             {
                                 var entries = registry._entries;
                                 var index = 0;
                                 for (; index < entries.Length && entries[index].Priority > 0; index++)
                                 {
                                     var value = entries[index].Provider[key];
                                     if (!global::System.String.IsNullOrEmpty(value))
                                     {
                                         return value;
                                     }
                                 }

                                 var ownValue = provider[key];
                                 if (!global::System.String.IsNullOrEmpty(ownValue))
                                 {
                                     return ownValue;
                                 }

                                 for (; index < entries.Length; index++)
                                 {
                                     var value = entries[index].Provider[key];
                                     if (!global::System.String.IsNullOrEmpty(value))
                                     {
                                         return value;
                                     }
                                 }

                                 return global::System.String.Empty;
                             }
                         }
                     }
                 }
                 """;
    }

    private string GenerateCompiledMainClass
    (
        LocalizationGeneratingModel model,
        ImmutableSortedDictionary<string, IReadOnlyList<LocalizationFileModel>> allLocalizationModels
    )
    {
        var isNestedSource = model.DependencyMode == DependencyMode.NestedSource;
        var supportsNotifyChanged = model.NotificationMode is not NotificationMode.InitOnly;
        using var builder = new SourceTextBuilder(model.Namespace);

        var allTags = allLocalizationModels.Keys.ToList();
        var currentLanguageExpression = model.CurrentLanguage is null
            ? "global::System.Globalization.CultureInfo.CurrentUICulture.Name"
            : $"\"{model.CurrentLanguage.ToLowerInvariant()}\"";

        var tagListLiteral = string.Join("\n", allTags.Select(t => $"    \"{t}\","));
        var switchArms = string.Join
        (
            "\n", allTags.Select
            (t =>
                $"    \"{t.ToLowerInvariant()}\" => {(isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.")}LocalizedValues_{IetfLanguageTagToIdentifier(t)}.Instance,"
            )
        );
        var fallbackExpression = isNestedSource
            ? "LocalizationFallbackHelper.FindBestMatch(languageTag, SupportedLanguageTags)"
            : "global::DotNetCampus.Localizations.Helpers.LocalizationHelper.MatchWithFallback(languageTag, SupportedLanguageTags)";
        var defaultArm = $"    _ => {fallbackExpression} is {{ }} fallback ? Create(fallback) : _default,";
        var switchBody = $"{switchArms}\n{defaultArm}";

        var defaultTagIdentifier = IetfLanguageTagToIdentifier(model.DefaultLanguage);
        var defaultExpression =
            $"{(isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.")}LocalizedValues_{defaultTagIdentifier}.Instance";
        var interfacePrefix = isNestedSource ? "" : $"global::{GeneratorInfo.RootNamespace}.";

        if (supportsNotifyChanged)
        {
            builder.AddTypeDeclaration
            (
                $"partial class {model.TypeName}", t => t
                    .AddRawMembers
                    (
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
                         public static {interfacePrefix}ILocalizedValues Current => _current;
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
                          """
                    )
            );
        }
        else
        {
            builder.AddTypeDeclaration
            (
                $"partial class {model.TypeName}", t => t
                    .AddRawMembers
                    (
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
                          """
                    )
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