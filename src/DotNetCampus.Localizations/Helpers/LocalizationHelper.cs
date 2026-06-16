using System.Collections.Generic;

namespace DotNetCampus.Localizations.Helpers;

/// <summary>
/// 提供本地化相关的帮助方法。
/// </summary>
public static class LocalizationHelper
{
    /// <summary>
    /// 基于 BCP-47 标准的语言标签匹配规则，匹配请求的语言标签和可用的语言标签，返回最合适的匹配项。
    /// </summary>
    /// <param name="requestedIetfLanguageTag">请求的语言标签。</param>
    /// <param name="availableIetfLanguageTags">可用的语言标签列表。</param>
    /// <returns></returns>
    public static string? MatchWithFallback(string requestedIetfLanguageTag, IEnumerable<string> availableIetfLanguageTags)
    {
        return LocalizationFallbackProvider.FindBestMatch(requestedIetfLanguageTag, availableIetfLanguageTags);
    }
}
