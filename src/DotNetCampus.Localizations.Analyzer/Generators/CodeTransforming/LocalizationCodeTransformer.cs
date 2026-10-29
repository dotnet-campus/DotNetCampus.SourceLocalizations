using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DotNetCampus.Localizations.Generators.ModelProviding;
using Microsoft.CodeAnalysis;

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
    /// 从通过重复键检查的本地化项创建转换器，并按同一比较规则构建 Key 树。
    /// </summary>
    private LocalizationCodeTransformer(ImmutableArray<LocalizationItem> items, StringComparer keyComparer)
    {
        LocalizationItems = items;
        KeyComparer = keyComparer;
        Tree = LocalizationTreeNode.FromList(items, keyComparer);
    }

    /// <summary>
    /// 读取同一语言的全部资源并检查重复 Key；输入冲突作为诊断返回，不进入未知异常处理。
    /// </summary>
    /// <param name="fileModels">同一语言下的所有资源文件，包含跨文件及跨格式的定义。</param>
    /// <param name="model">用户配置，提供 Key 比较规则及诊断位置。</param>
    /// <param name="transformer">成功时返回可用于生成代码的转换器。</param>
    /// <param name="diagnostic">失败时返回重复键诊断，由接口生成路径统一报告。</param>
    /// <returns>所有 Key 满足当前比较规则时返回 true；发生冲突时返回 false。</returns>
    internal static bool TryCreate(IReadOnlyList<LocalizationFileModel> fileModels, LocalizationGeneratingModel model,
        out LocalizationCodeTransformer transformer, out Diagnostic? diagnostic)
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
                diagnostic = Diagnostic.Create(Diagnostics.DLA007_DuplicateLocalizationKey, model.Location,
                    fileModels[0].IetfLanguageTag, existingKey, item.Key);
                transformer = null!;
                return false;
            }
            keys.Add(item.Key, item.Key);
        }

        // 重复键不能继续合并成树，否则会丢失定义，或在生成字典时再次抛出异常。
        transformer = new LocalizationCodeTransformer(items, model.KeyComparer);
        diagnostic = null;
        return true;
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
