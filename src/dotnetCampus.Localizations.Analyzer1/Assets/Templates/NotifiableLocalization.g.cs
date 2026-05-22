#nullable enable
using global::dotnetCampus.Localizations;

namespace dotnetCampus.Localizations.Assets.Templates;

/// <summary>
/// 支持属性变更通知的本地化字符串实现。
/// 生成的代码会在语言切换或属性变更时发出通知，适用于需要响应式UI更新的场景。
/// </summary>
partial class NotifiableLocalization
{
    /// <summary>
    /// 默认的不可变本地化字符串集实例。
    /// </summary>
    private static readonly ImmutableLocalizedValues _default = new ImmutableLocalizedValues(CreateLocalizedStringProvider("DEFAULT_IETF_LANGUAGE_TAG"));

    /// <summary>
    /// 当前的可通知本地化字符串集实例。
    /// 此实例支持属性变更通知，适用于UI绑定场景。
    /// </summary>
    private static NotifiableLocalizedValues _current;

    static NotifiableLocalization()
    {
        _current = new NotifiableLocalizedValues(CreateLocalizedStringProvider("CURRENT_IETF_LANGUAGE_TAG"));
    }

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
    /// 获取默认的本地化字符串集。
    /// </summary>
    /// <remarks>
    /// 此实例为不可变对象，不支持属性变更通知。
    /// </remarks>
    public static ILocalizedValues Default => _default;

    /// <summary>
    /// 获取当前的本地化字符串集。
    /// </summary>
    /// <remarks>
    /// 此实例支持属性变更通知，当调用 <see cref="SetCurrent(string)"/> 方法时，
    /// 会通过 INotifyPropertyChanged 接口通知所有绑定的属性已变更。
    /// </remarks>
    public static ILocalizedValues Current => _current;

    /// <summary>
    /// 设置当前的本地化字符串集。
    /// </summary>
    /// <param name="languageTag">要设置的语言标签（推荐 IETF 语言标签）。</param>
    /// <remarks>
    /// 此方法会更新当前实例的字符串提供器，并触发属性变更通知，
    /// 所有绑定到 <see cref="Current"/> 的UI元素将自动更新。
    /// </remarks>
    public static async global::System.Threading.Tasks.ValueTask SetCurrent(string languageTag)
    {
        await _current.SetProvider(CreateLocalizedStringProvider(languageTag));
    }

    /// <summary>
    /// 创建指定语言标签的本地化字符串集。
    /// </summary>
    /// <param name="languageTag">语言标签（推荐 IETF 语言标签）。</param>
    /// <returns>不可变的本地化字符串集实例。</returns>
    /// <remarks>
    /// 由于当前字符串集可变，所以此方法总是创建新实例，不会复用现有实例。
    /// </remarks>
    public static ILocalizedValues Create(string languageTag) => new ImmutableLocalizedValues(CreateLocalizedStringProvider(languageTag));

    /// <summary>
    /// 创建指定语言标签的本地化字符串提供器。
    /// </summary>
    /// <remarks>
    /// 如果找不到完全匹配的语言标签，将尝试找到父级语言标签的提供器。
    /// </remarks>
    /// <param name="languageTag">语言标签（推荐 IETF 语言标签）。</param>
    /// <returns>本地化字符串提供器。</returns>
    /// <exception cref="global::System.ArgumentException">当没有找到支持的语言标签时抛出。</exception>
    private static ILocalizedStringProvider CreateLocalizedStringProvider(string languageTag)
    {
        var lowerTag = languageTag.ToLowerInvariant();
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
