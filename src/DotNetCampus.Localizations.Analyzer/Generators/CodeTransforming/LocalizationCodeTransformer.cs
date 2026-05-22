using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DotNetCampus.Localizations.Assets.Templates;
using DotNetCampus.Localizations.Generators.ModelProviding;
using DotNetCampus.Localizations.IO;
using DotNetCampus.Localizations.Utils.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static DotNetCampus.Localizations.Generators.ModelProviding.IetfLanguageTagExtensions;

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
    /// <remarks>
    /// 这个属性所表示的节点是根节点，其键和值都是 null，但是其子节点包含了所有的本地化项。
    /// </remarks>
    private LocalizationTreeNode Tree { get; }

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
                .Reverse() // 后定义的项覆盖先定义的项
                .Distinct(LocalizationItem.KeyEqualityComparer),
        ];
        Tree = LocalizationTreeNode.FromList(LocalizationItems);
    }

    #region Language Key Interfaces

    public string ToInterfaceCodeText(LocalizationGeneratingModel model)
    {
        return EmbeddedSourceFile.Get<ILocalizedValues>().Content
            .Replace("namespace DotNetCampus.Localizations.Assets.Templates;", $"namespace {GeneratorInfo.RootNamespace};")
            .FlagReplace(string.Join("\n\n", string.Join("\n\n", GenerateInterfacePropertyLines(Tree))))
            .Flag2Replace(string.Concat(Tree.Children.Select(RecursiveConvertLocalizationTreeNodeToKeyInterfaceCode)));
    }

    private string RecursiveConvertLocalizationTreeNodeToKeyInterfaceCode(LocalizationTreeNode node)
    {
        if (node.Children.Count is 0)
        {
            return "";
        }

        var nodeTypeName = node.GetFullIdentifierKey("_");
        return $$"""

[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
public interface ILocalizedValues_{{nodeTypeName}}
{
{{string.Join("\n\n", GenerateInterfacePropertyLines(node))}}
}
{{string.Concat(node.Children.Select(x => RecursiveConvertLocalizationTreeNodeToKeyInterfaceCode(x)))}}
""";
    }

    private IEnumerable<string> GenerateInterfacePropertyLines(LocalizationTreeNode node)
    {
        return node.Children.Select(x =>
        {
            var identifierKey = x.GetFullIdentifierKey("_");
            if (x.Children.Count is 0)
            {
                if (x.Item.ValueArgumentTypes.Length is 0)
                {
                    return $$"""
    /// <summary>
    /// {{ConvertValueToComment(x.Item.SampleValue)}}
    /// </summary>
    LocalizedString {{x.IdentifierKey}} { get; }
""";
                }
                else
                {
                    var genericTypes = string.Join(", ", x.Item.ValueArgumentTypes);
                    return $$"""
    /// <summary>
    /// {{ConvertValueToComment(x.Item.SampleValue)}}
    /// </summary>
    LocalizedString<{{genericTypes}}> {{x.IdentifierKey}} { get; }
""";
                }
            }
            else
            {
                return $$"""
    ILocalizedValues_{{identifierKey}} {{x.IdentifierKey}} { get; }
""";
            }
        });
    }

    private string ConvertValueToComment(string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains('\n'))
        {
            return value ?? "";
        }

        var lines = value!.Replace("\r", "").Split('\n');
        return string.Join("<br/>\n    /// ", lines);
    }

    #endregion

    #region Language Value Implementations

    public string ToImplementationCodeText(LocalizationGeneratingModel model, bool isNotifiable)
    {
        var typePrefix = isNotifiable ? "Notifiable" : "Immutable";
        var content = isNotifiable
            ? EmbeddedSourceFile.Get<NotifiableLocalizedValues>().Content
            : EmbeddedSourceFile.Get<ImmutableLocalizedValues>().Content;
        return content
            .Replace("LOCALIZATION_TYPE_NAME", model.TypeName)
            .Replace("namespace DotNetCampus.Localizations.Assets.Templates;", $"namespace {GeneratorInfo.RootNamespace};")
            .FlagReplace(string.Join("\n", GenerateImplementationPropertyLines(Tree, typePrefix)))
            .Flag2Replace(string.Concat(Tree.Children.Select(x => RecursiveConvertLocalizationTreeNodeToKeyImplementationCode(x, model.TypeName, typePrefix, isNotifiable))))
            .Flag3Replace(GenerateSetProvider(Tree, typePrefix));
    }

    private string GenerateSetProvider(LocalizationTreeNode node, string typePrefix)
    {
        return $$"""

    /// <summary>
    /// 在不改变 <see cref="LocalizedStringProvider"/> 实例的情况下，设置新的本地化字符串提供器，并通知所有的属性的变更。
    /// </summary>
    /// <param name="newProvider">新的本地化字符串提供器。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="newProvider"/> 为 null 时抛出。</exception>
#pragma warning disable CS1998
    internal async global::System.Threading.Tasks.ValueTask SetProvider(ILocalizedStringProvider newProvider)
#pragma warning restore CS1998
    {
        if (newProvider is null)
        {
            throw new ArgumentNullException(nameof(newProvider));
        }

        var oldProvider = LocalizedStringProvider;
        if (oldProvider == newProvider)
        {
            return;
        }
        LocalizedStringProvider = newProvider;

        // 递归通知所有叶子节点引发变更事件。
{{string.Join("\n", GeneratePropertyChanged(node, typePrefix))}}
    }
""";
    }

    private string RecursiveConvertLocalizationTreeNodeToKeyImplementationCode(LocalizationTreeNode node, string typeName, string typePrefix, bool isNotifiable)
    {
        if (node.Children.Count is 0)
        {
            return "";
        }

        var nodeKeyName = node.GetFullIdentifierKey(".");
        var nodeTypeName = node.GetFullIdentifierKey("_");
        return $$"""

[global::System.Diagnostics.DebuggerDisplay("[{LocalizedStringProvider.IetfLanguageTag}] {{typeName}}.{{nodeKeyName}}.???")]
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
internal sealed partial class {{typePrefix}}LocalizedValues_{{nodeTypeName}}(ILocalizedStringProvider provider) : ILocalizedValues_{{nodeTypeName}}{{(isNotifiable ? ", INotifyPropertyChanged" : "")}}
{
    /// <summary>
    /// 获取本地化字符串提供器。
    /// </summary>
    public ILocalizedStringProvider LocalizedStringProvider {{(isNotifiable ? "{ get; private set; } =" : "=>")}} provider;
{{(isNotifiable ? GenerateSetProvider(node, typePrefix) : "")}}
{{string.Join("\n", GenerateImplementationPropertyLines(node, typePrefix))}}
{{(isNotifiable ? """

#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

""" : "")}}
    /// <summary>
    /// 获取非完整本地化字符串键的字符串表示。
    /// </summary>
    public override string ToString() => "{{typeName}}.{{nodeKeyName}}.";
}
{{string.Concat(node.Children.Select(x => RecursiveConvertLocalizationTreeNodeToKeyImplementationCode(x, typeName, typePrefix, isNotifiable)))}}
""";
    }

    private static IEnumerable<string> GenerateImplementationPropertyLines(LocalizationTreeNode node, string typePrefix)
    {
        return node.Children.Select(x =>
        {
            var identifierKey = x.GetFullIdentifierKey("_");
            if (x.Children.Count is 0)
            {
                if (x.Item.ValueArgumentTypes.Length is 0)
                {
                    return $"""

    /// <inheritdoc />
    public LocalizedString {x.IdentifierKey} => LocalizedStringProvider.Get0("{x.Item.Key}");
""";
                }
                else
                {
                    var genericTypes = string.Join(", ", x.Item.ValueArgumentTypes);
                    return $"""

    /// <inheritdoc />
    public LocalizedString<{genericTypes}> {x.IdentifierKey} => LocalizedStringProvider.Get{x.Item.ValueArgumentTypes.Length}<{genericTypes}>("{x.Item.Key}");
""";
                }
            }
            else
            {
                return $$"""

    public ILocalizedValues_{{identifierKey}} {{x.IdentifierKey}} { get; } = new {{typePrefix}}LocalizedValues_{{identifierKey}}(provider);
""";
            }
        });
    }

    private IEnumerable<string> GeneratePropertyChanged(LocalizationTreeNode node, string typePrefix)
    {
        return node.Children.Select(x =>
        {
            return x.Children.Count is 0
                ? $"        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(\"{x.IdentifierKey}\"));"
                : $"        await (({typePrefix}LocalizedValues_{x.GetFullIdentifierKey("_")}){x.IdentifierKey}).SetProvider(newProvider);";
        });
    }

    #endregion

    #region Language Value Provider

    public string ToProviderCodeText(string rootNamespace, string ietfLanguageTag)
    {
        var typeName = IetfLanguageTagToIdentifier(ietfLanguageTag);
        var template = EmbeddedSourceFile.Get<LocalizedStringProvider>();
        var code = template.Content
            .Replace($"namespace {template.Namespace};", $"namespace {GeneratorInfo.RootNamespace};")
            .Replace($"class {nameof(LocalizedStringProvider)}", $"class {nameof(LocalizedStringProvider)}_{typeName}")
            .Replace("""IetfLanguageTag => "default";""", $"""IetfLanguageTag => "{ietfLanguageTag}";""")
            .FlagReplace(string.Join("\n", LocalizationItems.Select(x => ConvertKeyValueToValueCodeLine(x.Key, x.Value))));
        return code;
    }

    private string ConvertKeyValueToValueCodeLine(string key, string value)
    {
        var escapedValue = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value)).ToFullString();
        return $"        {{ \"{key}\", {escapedValue} }},";
    }

    private string ConvertKeyValueToProperty(string key, string value)
    {
        return "";
    }

    private IEnumerable<string> EnumerateConvertTreeNodeToInterfaceNames(IEnumerable<LocalizationTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Children.Count is 0)
            {
                // 叶子节点，不提供接口。
            }
            else
            {
                // 非叶子节点，提供接口。
                yield return node.GetFullIdentifierKey("_");
                foreach (var child in EnumerateConvertTreeNodeToInterfaceNames(node.Children))
                {
                    yield return child;
                }
            }
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 格式无关的本地化项树节点。
    /// </summary>
    /// <param name="item">本地化项。</param>
    /// <param name="identifierKey">适用于 C# 标识符的当前节点的键。</param>
    /// <param name="identifierKeyParts">适用于 C# 标识符的从根到当前节点的完整键。</param>
    private class LocalizationTreeNode(LocalizationItem item, string identifierKey, ImmutableArray<string> identifierKeyParts)
    {
        /// <summary>
        /// 本地化项。
        /// </summary>
        public LocalizationItem Item => item;

        /// <summary>
        /// 适用于 C# 标识符的当前节点的键。
        /// </summary>
        public string IdentifierKey => identifierKey;

        /// <summary>
        /// 适用于 C# 标识符的当前节点的完整键（包含从根到此节点的完整键路径，以“<paramref name="separator"/>”分隔）。
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
                var child = current.Children.FirstOrDefault(x => x.IdentifierKey == part);
                if (child is null)
                {
                    child = new LocalizationTreeNode(localizationItem, part, [..parts.Take(i + 1)]);
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
            var root = new LocalizationTreeNode(default, default!, default!);
            foreach (var item in localizationItemList)
            {
                var keyParts = item.Key.Split(['.'], StringSplitOptions.RemoveEmptyEntries);

                if (keyParts.Length is 0)
                {
                    continue;
                }

                if (keyParts.Length is 1)
                {
                    root.Children.Add(new LocalizationTreeNode(item, item.Key, [item.Key]));
                    continue;
                }

                _ = root.GetOrCreateDescendant(item);
            }
            return root;
        }
    }

    #endregion
}
