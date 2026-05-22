using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace dotnetCampus.Localizations.Generators.ModelProviding;

/// <summary>
/// IETF 语言标签扩展方法。
/// </summary>
public static class IetfLanguageTagExtensions
{
    /// <summary>
    /// 将 IETF 语言标签转换为适用于 C# 标识符的字符串。
    /// </summary>
    /// <param name="ietfLanguageTag">IETF 语言标签。</param>
    /// <returns>适用于 C# 标识符的字符串。</returns>
    public static string IetfLanguageTagToIdentifier(string ietfLanguageTag)
    {
        Span<char> identifier = stackalloc char[ietfLanguageTag.Length];
        var isPartStart = true;
        var identifierIndex = 0;

        foreach (var c in ietfLanguageTag)
        {
            if (c is '-')
            {
                isPartStart = true;
                continue;
            }

            if (isPartStart)
            {
                identifier[identifierIndex++] = char.ToUpperInvariant(c);
                isPartStart = false;
            }
            else
            {
                identifier[identifierIndex++] = char.ToLowerInvariant(c);
            }
        }

        return identifier.Slice(0, identifierIndex).ToString();
    }

    /// <summary>
    /// 将 <see cref="LocalizationFileModel"/> 按照 IETF 语言标签分组。
    /// </summary>
    /// <param name="models">要分组的 <see cref="LocalizationFileModel"/> 集合。</param>
    /// <param name="supportsNonIetfLanguageTag">是否支持非 IETF 语言标签。</param>
    /// <returns>枚举的每一项都是一个元组，包含 IETF 语言标签和对应的 <see cref="LocalizationFileModel"/> 集合。</returns>
    public static IEnumerable<(string IetfLanguageTag, IReadOnlyList<LocalizationFileModel> Models)> GroupByIetfLanguageTag(
        this IEnumerable<LocalizationFileModel> models, bool supportsNonIetfLanguageTag)
    {
        var groups = new Dictionary<string, List<LocalizationFileModel>>(StringComparer.OrdinalIgnoreCase);

        foreach (var model in models)
        {
            if (!supportsNonIetfLanguageTag && !IsIetfLanguageTag(model.IetfLanguageTag))
            {
                continue;
            }

            if (!groups.TryGetValue(model.IetfLanguageTag, out var group))
            {
                group = [];
                groups[model.IetfLanguageTag] = group;
            }

            group.Add(model);
        }

        foreach (var group in groups.Values)
        {
            yield return (group[0].IetfLanguageTag, [..group]);
        }
    }

    /// <summary>
    /// 从文件名中猜测到底哪一段才是 IETF 语言标签。
    /// </summary>
    /// <param name="fileNameWithoutExtension">没有扩展名的文件名。</param>
    /// <returns>如果能从文件名中猜测出 IETF 语言标签，则返回它，否则返回 null。</returns>
    public static string GuessIetfLanguageTagFromFileName(string fileNameWithoutExtension)
    {
        var parts = fileNameWithoutExtension.Split([' ', '.', '_', ',', ';'], StringSplitOptions.RemoveEmptyEntries);
        return parts.LastOrDefault(IsIetfLanguageTag) ?? fileNameWithoutExtension;
    }

    /// <summary>
    /// 判断一个字符串是否是 IETF 语言标签。
    /// </summary>
    /// <param name="text">要判断的字符串。</param>
    /// <returns>如果是 IETF 语言标签，则返回 true，否则返回 false。</returns>
    private static bool IsIetfLanguageTag(string text)
    {
        try
        {
            var cultureInfo = new CultureInfo(text);
            return cultureInfo.Name.Equals(text, StringComparison.OrdinalIgnoreCase);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
