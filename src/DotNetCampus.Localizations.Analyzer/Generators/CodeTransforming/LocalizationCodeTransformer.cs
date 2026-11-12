using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DotNetCampus.Localizations.Generators.ModelProviding;
using DotNetCampus.Localizations.Utils.CodeAnalysis;

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
    /// 本次转换使用的 Key 比较器，供后续值映射复用。
    /// </summary>
    internal StringComparer KeyComparer { get; }

    /// <summary>
    /// 从通过重复键检查的语言项创建转换器，并按同一比较规则构建 Key 树。
    /// </summary>
    private LocalizationCodeTransformer(ImmutableArray<LocalizationItem> items, StringComparer keyComparer)
    {
        LocalizationItems = items;
        KeyComparer = keyComparer;
        Tree = LocalizationTreeNode.FromList(items, keyComparer);
    }

    /// <summary>
    /// 读取同一语言的全部资源，检查重复 Key 并创建转换器。
    /// </summary>
    /// <param name="fileModels">同一语言下的所有资源文件，包含跨文件及跨格式的定义。</param>
    /// <param name="model">用户配置，提供 Key 比较规则及诊断位置。</param>
    /// <exception cref="DiagnosticsException">存在重复 Key，由接口生成器统一报告。</exception>
    internal static LocalizationCodeTransformer Create(IReadOnlyList<LocalizationFileModel> fileModels, LocalizationGeneratingModel model)
    {
        ImmutableArray<LocalizationItem> items =
        [
            ..fileModels
                .Select(x => (Content: x.Content, Reader: (x.FileFormat switch
                {
                    "toml" => (ILocalizationFileReader)new TomlLocalizationFileReader(),
                    "yaml" => (ILocalizationFileReader)new YamlLocalizationFileReader(),
                    _ => throw new NotSupportedException($"Unsupported localization file format: {x.FileFormat}"),
                })))
                .SelectMany(x => x.Reader.Read(x.Content))
                .Reverse(),
        ];

        var keys = new Dictionary<string, string>(model.KeyComparer);
        foreach (var item in items)
        {
            if (keys.TryGetValue(item.Key, out var existingKey))
            {
                throw new DiagnosticsException(Diagnostics.DLA007_DuplicateLocalizationKey, model.Location,
                    fileModels[0].IetfLanguageTag, existingKey, item.Key);
            }
            keys.Add(item.Key, item.Key);
        }

        return new LocalizationCodeTransformer(items, model.KeyComparer);
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
