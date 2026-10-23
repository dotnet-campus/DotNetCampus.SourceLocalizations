using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Tomlet;
using Tomlet.Models;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

/// <summary>
/// 读取 TOML 格式的本地化文件。
/// </summary>
/// <remarks>
/// 将嵌套表展开为点号分隔的本地化键，并通过统一的文案转换器处理数字、命名及带类型标注的占位符。
/// </remarks>
public class TomlLocalizationFileReader : ILocalizationFileReader
{
    /// <inheritdoc />
    /// <remarks>字符串作为本地化文案读取，表节点递归展开；其他 TOML 值类型不作为本地化文案处理。</remarks>
    public ImmutableArray<LocalizationItem> Read(string content)
    {
        var parser = new TomlParser();
        var document = parser.Parse(content);

        // 从顶层条目递归展开所有字符串，统一返回与文件层级无关的本地化项。
        return [..document.Entries.SelectMany(x => ParseEntry(x.Key, x.Value))];
    }

    /// <summary>
    /// 将 TOML 条目转换为本地化项；遇到表节点时继续展开其子条目。
    /// </summary>
    /// <param name="key">包含父级路径的本地化键，层级之间使用点号分隔。</param>
    /// <param name="tomlValue">TOML 解析器提供的字符串或表节点。</param>
    /// <returns>当前字符串对应的本地化项，或表中递归展开的所有本地化项。</returns>
    /// <exception cref="NotSupportedException">条目不是字符串或表，例如数字、布尔值或数组。</exception>
    private static IEnumerable<LocalizationItem> ParseEntry(string key, TomlValue tomlValue)
    {
        if (tomlValue is TomlString tomlString)
        {
            // 生成代码使用转换后的文案和参数类型，同时保留原文案供示例及文档注释使用。
            var (types, value) = LocalizationValueConverter.ConvertToCodeValue(tomlString.Value);
            yield return new LocalizationItem(
                key,
                value,
                tomlString.Value,
                types,
                tomlString.Comments.InlineComment);
        }
        else if (tomlValue is TomlTable tomlTable)
        {
            // 将父级键传给下一层，使表中的字符串与直接声明的点号键使用相同的键结构。
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
}
