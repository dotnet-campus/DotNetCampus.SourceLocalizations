using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Tomlet;
using Tomlet.Models;

namespace dotnetCampus.Localizations.Generators.CodeTransforming;

/// <summary>
/// 读取 TOML 格式的本地化文件。
/// </summary>
public class TomlLocalizationFileReader : ILocalizationFileReader
{
    public ImmutableArray<LocalizationItem> Read(string content)
    {
        var parser = new TomlParser();
        var document = parser.Parse(content);
        return [..document.Entries.SelectMany(x => ParseEntry(x.Key, x.Value))];
    }

    private static IEnumerable<LocalizationItem> ParseEntry(string key, TomlValue tomlValue)
    {
        if (tomlValue is TomlString tomlString)
        {
            var (types, value) = ConvertTomlValueToCodeValue(tomlString.Value);
            yield return new LocalizationItem(
                key,
                value,
                tomlString.Value,
                types,
                tomlString.Comments.InlineComment);
        }
        else if (tomlValue is TomlTable tomlTable)
        {
            foreach (var pair in tomlTable.Entries)
            {
                var (k, v) = (pair.Key, pair.Value);
                foreach (var item in ParseEntry($"{key}.{k}", v))
                {
                    yield return item;
                }
            }
        }
        else
        {
            throw new NotSupportedException($"Unsupported TOML value type: {tomlValue.GetType()}");
        }
    }

    /// <summary>
    /// 将 TOML 文件中的语言项值转换为适用于 C# 代码中可格式化字符串的值。
    /// </summary>
    /// <example>
    /// 从 TOML 文件中读取的语言项值为 <c>"{name:string} is {age:int} years old."</c>，转换为 <c>([string, int],"{0} is {1} years old.")</c>。
    /// </example>
    /// <param name="value">TOML 文件中的语言项值。</param>
    /// <returns>适用于 C# 代码中可格式化字符串的值。</returns>
    private static (ImmutableArray<string> Types, string Value) ConvertTomlValueToCodeValue(string value)
    {
        var regex = new Regex(@"\{(?<name>[^{}:]+)(?::(?<type>[^{}:]+))?\}");
        var matches = regex.Matches(value);
        ImmutableArray<string> types = matches.Count is 0
            ? []
            : [..matches.OfType<Match>().Select(x => x.Groups["type"].Success ? x.Groups["type"].Value : "object")];
        var index = 0;
        var v = regex.Replace(value, _ => $"{{{index++}}}");
        return (types, v);
    }
}
