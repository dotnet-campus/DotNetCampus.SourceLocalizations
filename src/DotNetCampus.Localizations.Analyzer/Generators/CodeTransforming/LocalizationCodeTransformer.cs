using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DotNetCampus.Localizations.Generators.ModelProviding;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

/// <summary>
/// 提供一个与语言文件格式无关的本地化项到 C# 代码的转换器。
/// </summary>
public class LocalizationCodeTransformer
{
    /// <summary>
    /// 获取所有的本地化项。
    /// </summary>
    public ImmutableArray<LocalizationItem> LocalizationItems { get; }

    /// <summary>
    /// 以树形式表示的所有本地化项。
    /// </summary>
    internal LocalizationTreeNode Tree { get; }

    /// <summary>
    /// 创建 <see cref="LocalizationCodeTransformer"/> 的新实例。
    /// </summary>
    /// <param name="fileModels">读取出来的所有语言项。</param>
    public LocalizationCodeTransformer(IReadOnlyList<LocalizationFileModel> fileModels)
    {
        LocalizationItems =
        [
            ..fileModels
                .Select(x => (Content: x.Content, Reader: (x.FileFormat switch
                {
                    "toml" => (ILocalizationFileReader)new TomlLocalizationFileReader(),
                    "yaml" => (ILocalizationFileReader)new YamlLocalizationFileReader(),
                    _ => throw new NotSupportedException($"Unsupported localization file format: {x.FileFormat}"),
                })))
                .SelectMany(x => x.Reader.Read(x.Content))
                .Reverse()
                .Distinct(LocalizationItem.KeyEqualityComparer),
        ];
        Tree = LocalizationTreeNode.FromList(LocalizationItems);
    }

    internal IEnumerable<LocalizationTreeNode> EnumerateAllNonLeafDescendants(LocalizationTreeNode root)
    {
        foreach (var child in root.Children)
        {
            if (child.Type == LocalizationTreeNodeType.Category)
            {
                yield return child;
                foreach (var descendant in EnumerateAllNonLeafDescendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    internal IEnumerable<LocalizationTreeNode> EnumerateAllLeaves(LocalizationTreeNode root)
    {
        foreach (var child in root.Children)
        {
            if (child.Type == LocalizationTreeNodeType.Leaf)
            {
                yield return child;
            }
            else
            {
                foreach (var leaf in EnumerateAllLeaves(child))
                {
                    yield return leaf;
                }
            }
        }
    }

    internal string ConvertValueToComment(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var escaped = value!
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("*/", "*&#47;");

        if (!escaped.Contains('\n'))
        {
            return escaped;
        }

        var lines = escaped.Replace("\r\n", "\n").Split('\n');
        return string.Join("<br/>\n    /// ", lines);
    }
}
