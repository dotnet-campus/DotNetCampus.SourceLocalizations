#nullable enable
using global::dotnetCampus.Localizations;
using StringComparison = global::System.StringComparison;
using System;

namespace dotnetCampus.Localizations.Assets.Templates;

/// <summary>
/// 不可变的本地化字符串实现。
/// 生成的代码不会在属性变更时发出通知，适用于不需要响应式UI更新的场景。
/// </summary>
partial class ImmutableLocalization
{
    /// <summary>
    /// 默认的不可变本地化字符串集实例。
    /// </summary>
    private static readonly ImmutableLocalizedValues _default = GetOrCreateLocalizedValues("DEFAULT_IETF_LANGUAGE_TAG");

    /// <summary>
    /// 当前的不可变本地化字符串集实例。
    /// </summary>
    private static ImmutableLocalizedValues _current;

    static ImmutableLocalization()
    {
        _current = GetOrCreateLocalizedValues("CURRENT_IETF_LANGUAGE_TAG");
    }

    /// <summary>
    /// 获取默认的本地化字符串集。
    /// </summary>
    /// <remarks>
    /// 此实例为不可变对象，切换语言不会影响该实例内容。
    /// </remarks>
    public static ILocalizedValues Default => _default;

    /// <summary>
    /// 获取当前的本地化字符串集。
    /// </summary>
    /// <remarks>
    /// 调用 <see cref="SetCurrent(string)"/> 时，此属性会返回新的实例，但不会通知属性变更。
    /// </remarks>
    public static ILocalizedValues Current => _current;

    /// <summary>
    /// 获取支持的语言标签。
    /// </summary>
    /// <remarks>
    /// 由于项目中可以设置 LocalizationSupportsNonIetfLanguageTag 属性，所以不一定是 IETF 语言标签。
    /// </remarks>
    public static System.Collections.Generic.IReadOnlyList<string> SupportedLanguageTags { get; } =
    [
        // <FLAG2>
        "en",
        // </FLAG2>
    ];

    /// <summary>
    /// 设置当前的本地化字符串集。
    /// </summary>
    /// <remarks>
    /// 此方法会创建并替换整个当前实例，但不会发出属性变更通知。
    /// 需要获取新值时，须重新访问 <see cref="Current"/> 属性。
    /// </remarks>
    /// <param name="languageTag">要设置的语言标签（推荐 IETF 语言标签）。</param>
    public static void SetCurrent(string languageTag)
    {
        _current = (ImmutableLocalizedValues)Create(languageTag);
    }

    /// <summary>
    /// 创建指定语言标签的本地化字符串集。（如果刚好是默认或当前的语言标签，则直接返回默认或当前的本地化字符串集的实例。）
    /// </summary>
    /// <param name="languageTag">语言标签（推荐 IETF 语言标签）。</param>
    /// <returns>不可变的本地化字符串集实例。</returns>
    /// <remarks>
    /// 由于默认和当前字符串集不可变，所以此方法会尽可能复用现有实例。
    /// </remarks>
    public static ILocalizedValues Create(string languageTag) => GetOrCreateLocalizedValues(languageTag);

    /// <summary>
    /// 创建或获取指定语言标签的不可变本地化字符串集。
    /// </summary>
    /// <remarks>
    /// 如果刚好是默认或当前的语言标签，则直接返回已有实例，提高性能。
    /// </remarks>
    /// <param name="languageTag">语言标签（推荐 IETF 语言标签）。</param>
    /// <returns>不可变的本地化字符串集实例。</returns>
    private static ImmutableLocalizedValues GetOrCreateLocalizedValues(string languageTag)
    {
        if (_default is { } @default && languageTag.Equals("DEFAULT_IETF_LANGUAGE_TAG", StringComparison.OrdinalIgnoreCase))
        {
            return @default;
        }
        if (_current is { } current && languageTag.Equals(current.LocalizedStringProvider.IetfLanguageTag, StringComparison.OrdinalIgnoreCase))
        {
            return current;
        }
        return new ImmutableLocalizedValues(GetOrCreateLocalizedStringProvider(languageTag));
    }

    /// <summary>
    /// 创建指定语言标签的本地化字符串提供器。
    /// </summary>
    /// <remarks>
    /// 如果找不到完全匹配的语言标签，将尝试找到父级语言标签的提供器。
    /// </remarks>
    /// <param name="languageTag">语言标签（推荐 IETF 语言标签）。</param>
    /// <returns>本地化字符串提供器。</returns>
    /// <exception cref="global::System.ArgumentException">当没有找到支持的语言标签时抛出。</exception>
    private static ILocalizedStringProvider GetOrCreateLocalizedStringProvider(string languageTag)
    {
        var lowerTag = languageTag.ToLowerInvariant();
        if (_default is { } @default && lowerTag == "DEFAULT_IETF_LANGUAGE_TAG")
        {
            return @default.LocalizedStringProvider;
        }
        if (_current is { } current && languageTag.Equals(current.LocalizedStringProvider.IetfLanguageTag, StringComparison.OrdinalIgnoreCase))
        {
            return current.LocalizedStringProvider;
        }
        var provider = CreateLocalizedStringProviderCore(languageTag);
        if (provider is not null)
        {
            return provider;
        }
        var fallbackTag = global::dotnetCampus.Localizations.Helpers.LocalizationHelper.MatchWithFallback(languageTag, SupportedLanguageTags);
        provider = fallbackTag is null ? null : CreateLocalizedStringProviderCore(fallbackTag);
        if (provider is not null)
        {
            return provider;
        }
        return _default.LocalizedStringProvider;
    }

    /// <summary>
    /// 创建指定语言标签的本地化字符串提供器核心实现。
    /// </summary>
    /// <param name="languageTag">语言标签（推荐 IETF 语言标签）。</param>
    /// <returns>特定语言的字符串提供器，如果不支持则返回 <see langref="null"/>。</returns>
    private static ILocalizedStringProvider? CreateLocalizedStringProviderCore(string languageTag)
    {
        return languageTag.ToLowerInvariant() switch
        {
            // <FLAG>
            "en" => null,
            // </FLAG>
            _ => null,
        };
    }
}
