using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

/// <summary>
/// 将 TOML、YAML 中的本地化文案转换为 C# 可格式化字符串，并提供按参数索引排列的类型信息。
/// </summary>
/// <remarks>
/// <para>数字占位符保留原始索引，同一索引可以重复出现，因此不同语言可以独立调整文案中的参数顺序。</para>
/// <para>命名占位符仍按出现顺序分配参数；与数字占位符混用时，从最大数字索引之后开始分配，避免参数冲突。</para>
/// <para>占位符冒号后的内容表示 C# 类型名，用于生成泛型参数，不是数值或日期的格式说明。</para>
/// </remarks>
internal static class LocalizationValueConverter
{
    /// <summary>
    /// 本地化字符串支持的最大参数数量；读取器的索引检查与嵌套泛型类型生成共用此上限。
    /// </summary>
    internal const int MaxArgumentCount = 8;

    /// <summary>
    /// 解析文案中的占位符，保留数字索引、分配命名参数索引，并移除占位符中的类型标注。
    /// </summary>
    /// <param name="value">从本地化文件中读取的原始文案。</param>
    /// <returns>
    /// 按参数索引排列的类型数组，以及转换后的可格式化字符串。
    /// 未声明类型或未使用的索引位置以 <c>object</c> 补齐；没有占位符时返回空类型数组和原文案。
    /// </returns>
    /// <example>
    /// <para><c>"{1:string} belongs to group {0:int}."</c> 转换为 <c>([int, string], "{1} belongs to group {0}.")</c>。</para>
    /// <para><c>"{0:int}, again {0}."</c> 转换为 <c>([int], "{0}, again {0}.")</c>，重复出现不会增加参数数量。</para>
    /// </example>
    /// <exception cref="OverflowException">数字索引超出 <see cref="int"/> 的取值范围。</exception>
    /// <exception cref="NotSupportedException">数字索引或分配给命名占位符的索引超过支持的参数数量。</exception>
    /// <exception cref="FormatException">同一数字索引声明了不同的参数类型。</exception>
    public static (ImmutableArray<string> Types, string Value) ConvertToCodeValue(string value)
    {
        // 先区分数字索引与参数名称，再读取可选的类型名，避免将溢出的数字错误地当成命名参数重新编号。
        var regex = new Regex(@"\{(?:(?<index>[0-9]+)|(?<name>[^{}:]+))(?::(?<type>[^{}:]+))?\}");
        var indices = regex.Matches(value).OfType<Match>()
            .Select(match => match.Groups["index"].Success
                ? int.Parse(match.Groups["index"].Value, NumberStyles.None, CultureInfo.InvariantCulture)
                : -1)
            .ToArray();

        // 预扫描全部占位符，保留文案后面才出现的数字索引；在分配类型槽位前检查上界，避免异常索引导致大量内存分配。
        var nextIndex = indices.DefaultIfEmpty(-1).Max();
        if (nextIndex >= MaxArgumentCount)
        {
            throw new NotSupportedException($"Localization strings support at most {MaxArgumentCount} arguments.");
        }

        // 命名占位符排在最大数字索引之后；没有数字占位符时，仍从 0 开始按出现顺序分配。
        nextIndex++;

        // null 表示未声明类型，必须与显式的 object 区分，才能使重复参数的类型冲突检查与出现顺序无关。
        var types = new List<string?>();

        // position 只对应预扫描结果中的匹配位置，不是格式化字符串的参数编号。
        var position = 0;
        var converted = regex.Replace(value, match =>
        {
            // 数字占位符直接复用原索引，只有命名占位符才分配新的索引。
            var index = indices[position++];
            if (index < 0)
            {
                index = nextIndex++;
            }

            // 即使已有数字索引合法，后续追加的命名参数仍可能超过泛型类型支持的参数数量。
            if (index >= MaxArgumentCount)
            {
                throw new NotSupportedException($"Localization strings support at most {MaxArgumentCount} arguments.");
            }

            // 按实际索引补齐参数槽位，例如只出现 {2} 时也必须保留索引 0、1 对应的调用参数。
            while (types.Count <= index)
            {
                types.Add(null);
            }

            // 同一索引共用类型声明；未标注类型的重复占位符不会覆盖已有声明，不同的显式声明则报错。
            if (match.Groups["type"].Success)
            {
                var type = match.Groups["type"].Value;
                if (types[index] is not null && types[index] != type)
                {
                    throw new FormatException($"Conflicting types for localization argument {index}.");
                }
                types[index] = type;
            }

            // 输出只包含索引的标准占位符，重复或乱序出现时也不按匹配位置重新编号。
            return $"{{{index}}}";
        });

        // 未声明类型的参数和稀疏索引留下的空位统一使用 object，数组长度就是生成方法所需的参数数量。
        return ([..types.Select(type => type ?? "object")], converted);
    }
}
