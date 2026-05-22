using System.Text.RegularExpressions;

namespace dotnetCampus.Localizations.Utils.CodeAnalysis;

/// <summary>
/// 为生成源代码提供正则表达式。
/// </summary>
public static class TemplateRegexes
{
    private static Regex? _typeRegex;
    private static Regex? _flagRegex;
    private static Regex? _flag2Regex;
    private static Regex? _flag3Regex;

    /// <summary>
    /// 匹配类型名称。
    /// </summary>
    /// <example>
    /// 对于 <c>class MyClass</c>，匹配到 <c>MyClass</c>。
    /// </example>
    /// <remarks>
    /// 类型可以是 <c>class</c>、<c>record</c>、<c>struct</c>、<c>enum</c>、<c>interface</c>。
    /// </remarks>
    public static Regex TypeRegex => _typeRegex ??= GetTypeRegex();

    /// <summary>
    /// 匹配代码中的 // <FLAG>...</FLAG> 注释。这些注释出现在代码中用于指示即将在这里生成一些代码。
    /// </summary>
    public static Regex FlagRegex => _flagRegex ??= GetFlagRegex();

    /// <summary>
    /// 匹配代码中的 // <FLAG2>...</FLAG2> 注释。这些注释出现在代码中用于指示即将在这里生成一些代码。
    /// </summary>
    public static Regex Flag2Regex => _flag2Regex ??= GetFlag2Regex();

    /// <summary>
    /// 匹配代码中的 // <FLAG3>...</FLAG3> 注释。这些注释出现在代码中用于指示即将在这里生成一些代码。
    /// </summary>
    public static Regex Flag3Regex => _flag3Regex ??= GetFlag3Regex();

    private static Regex GetFlagRegex() => _flagRegex ??= new Regex(@"(?<=\n)\s+// <FLAG>.+?</FLAG>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static Regex GetFlag2Regex() => _flag2Regex ??= new Regex(@"(?<=\n)\s+// <FLAG2>.+?</FLAG2>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static Regex GetFlag3Regex() => _flag3Regex ??= new Regex(@"(?<=\n)\s+// <FLAG3>.+?</FLAG3>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static Regex GetTypeRegex() => _typeRegex ??= new Regex(@"\b(?:class|record|struct|enum|interface)\s([\w_]+)\b", RegexOptions.Compiled);

    /// <summary>
    /// 替换代码中的 // <FLAG>...</FLAG> 注释，将其替换为指定的内容。
    /// </summary>
    /// <param name="content">包含要替换的代码的字符串。</param>
    /// <param name="flagContent">要替换的内容。</param>
    /// <returns>替换后的字符串。</returns>
    public static string FlagReplace(this string content, string flagContent)
    {
        return FlagRegex.Replace(content, flagContent);
    }

    /// <summary>
    /// 替换代码中的 // <FLAG2>...</FLAG2> 注释，将其替换为指定的内容。
    /// </summary>
    /// <param name="content">包含要替换的代码的字符串。</param>
    /// <param name="flagContent">要替换的内容。</param>
    /// <returns>替换后的字符串。</returns>
    public static string Flag2Replace(this string content, string flagContent)
    {
        return Flag2Regex.Replace(content, flagContent);
    }

    /// <summary>
    /// 替换代码中的 // <FLAG3>...</FLAG3> 注释，将其替换为指定的内容。
    /// </summary>
    /// <param name="content">包含要替换的代码的字符串。</param>
    /// <param name="flagContent">要替换的内容。</param>
    /// <returns>替换后的字符串。</returns>
    public static string Flag3Replace(this string content, string flagContent)
    {
        return Flag3Regex.Replace(content, flagContent);
    }
}
