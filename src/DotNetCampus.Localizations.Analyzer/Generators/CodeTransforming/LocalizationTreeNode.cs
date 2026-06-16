using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

internal enum LocalizationTreeNodeType
{
    Root,
    Category,
    Leaf,
}

/// <summary>
/// 格式无关的本地化项树节点。
/// </summary>
/// <param name="item">本地化项。仅 Leaf 节点持有有效值，Root 和 Category 节点的 Item 为 default。</param>
/// <param name="identifierKey">适用于 C# 标识符的当前节点的键。Root 节点此值为空字符串。</param>
/// <param name="identifierKeyParts">适用于 C# 标识符的从根到当前节点的完整键。Root 节点此值为空数组。</param>
/// <param name="isRoot">是否为根节点。</param>
internal class LocalizationTreeNode(LocalizationItem item, string identifierKey, ImmutableArray<string> identifierKeyParts, bool isRoot = false)
{
    public LocalizationTreeNodeType Type => isRoot
        ? LocalizationTreeNodeType.Root
        : Children.Count > 0
            ? LocalizationTreeNodeType.Category
            : LocalizationTreeNodeType.Leaf;

    /// <summary>
    /// 本地化项。仅 Leaf 节点可访问此属性。
    /// </summary>
    public LocalizationItem Item
    {
        get
        {
            if (Type is not LocalizationTreeNodeType.Leaf)
            {
                throw new InvalidOperationException($"当前节点类型为 {Type}，不允许访问 Item 属性。只有 Leaf 节点才具有本地化项。");
            }
            return item;
        }
    }

    /// <summary>
    /// 适用于 C# 标识符的当前节点的键。Root 节点不可访问此属性。
    /// </summary>
    public string IdentifierKey
    {
        get
        {
            if (Type is LocalizationTreeNodeType.Root)
            {
                throw new InvalidOperationException($"当前节点类型为 Root，不允许访问 IdentifierKey 属性。");
            }
            return identifierKey;
        }
    }

    /// <summary>
    /// 适用于 C# 标识符的当前节点的完整键（包含从根到此节点的完整键路径，以"<paramref name="separator"/>"分隔）。
    /// </summary>
    public string GetFullIdentifierKey(string separator)
    {
        return string.Join(separator, identifierKeyParts);
    }

    /// <summary>
    /// 子节点。
    /// </summary>
    public List<LocalizationTreeNode> Children { get; } = [];

    /// <summary>
    /// 寻找本地化项在树中的节点，如果不存在则创建。
    /// </summary>
    /// <param name="localizationItem">本地化项。</param>
    /// <returns>本地化项在树中的节点。</returns>
    public LocalizationTreeNode GetOrCreateDescendant(LocalizationItem localizationItem)
    {
        var parts = localizationItem.Key.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        var current = this;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var child = current.Children.FirstOrDefault(
                x => string.Equals(x.IdentifierKey, part, StringComparison.OrdinalIgnoreCase));
            if (child is null)
            {
                var isLeaf = i == parts.Length - 1;
                child = new LocalizationTreeNode(
                    isLeaf ? localizationItem : default,
                    part,
                    [..parts.Take(i + 1)]);
                current.Children.Add(child);
            }
            current = child;
        }
        return current;
    }

    /// <summary>
    /// 从本地化项列表创建树。
    /// </summary>
    /// <param name="localizationItemList">本地化项列表。</param>
    /// <returns>树的根节点。</returns>
    public static LocalizationTreeNode FromList(IReadOnlyList<LocalizationItem> localizationItemList)
    {
        var root = new LocalizationTreeNode(default, "", [], isRoot: true);
        foreach (var item in localizationItemList)
        {
            if (item.Key.Split(['.'], StringSplitOptions.RemoveEmptyEntries).Length is 0)
            {
                continue;
            }

            root.GetOrCreateDescendant(item);
        }
        return root;
    }
}
