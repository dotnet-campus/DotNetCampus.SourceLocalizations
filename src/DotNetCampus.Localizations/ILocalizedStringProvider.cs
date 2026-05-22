namespace dotnetCampus.Localizations;

/// <summary>
/// 为源生成器生成的本地化字符串提供统一的访问接口。
/// </summary>
public interface ILocalizedStringProvider
{
    /// <summary>
    /// 获取符合 IETF 规范的语言标签。
    /// </summary>
    public string IetfLanguageTag { get; }

    /// <summary>
    /// 获取指定键的本地化字符串。
    /// </summary>
    /// <param name="key">要获取的本地化字符串的键。</param>
    /// <returns>一个本地化字符串。如果字符串包含占位符，则返回的字符串会包含形如 "{0}" "{1}" 这样的占位符用来格式化。</returns>
    string this[string key] { get; }
}
