using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DotNetCampus.Localizations.Generators.Builders;
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
        using var builder = new SourceTextBuilder(GeneratorInfo.RootNamespace);
        builder
            .Using("DotNetCampus.Localizations")
            .UsingTypeAlias("ILocalizedStringProvider", "DotNetCampus.Localizations.ILocalizedStringProvider")
            .UsingTypeAlias("LocalizedString", "DotNetCampus.Localizations.LocalizedString");
        AddInterfaceDeclarations(builder, model.TypeAccessibility, includeBaseInterface: true);
        return builder.ToString();
    }

    public string ToNestedInterfaceCodeText(LocalizationGeneratingModel model)
    {
        using var builder = new SourceTextBuilder(model.Namespace);
        builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper =>
        {
            AddInterfaceDeclarations(wrapper, "public", includeBaseInterface: false);
        });
        return builder.ToString();
    }

    private void AddInterfaceDeclarations(IAllowTypeDeclaration builder, string accessibility, bool includeBaseInterface)
    {
        builder.AddTypeDeclaration($"{accessibility} partial interface ILocalizedValues", t => t
            .WithSummaryComment("提供本地化字符串的访问接口。通过属性导航访问各分组和叶子节点的本地化值。")
            .AddGeneratedToolAndEditorBrowsingAttributes()
            .If(includeBaseInterface, t => t.AddBaseTypes("ILocalizedStringProvider"))
            .AddRawMembers(GenerateInterfacePropertyMembers(Tree))
        );

        foreach (var node in EnumerateAllNonLeafDescendants(Tree))
        {
            var nodeTypeName = node.GetFullIdentifierKey("_");
            var nodeKeyName = node.GetFullIdentifierKey(".");
            builder.AddTypeDeclaration($"{accessibility} interface ILocalizedValues_{nodeTypeName}", t => t
                .WithSummaryComment($"提供 {nodeKeyName} 分组下本地化字符串的访问接口。")
                .AddGeneratedToolAndEditorBrowsingAttributes()
                .AddRawMembers(GenerateInterfacePropertyMembers(node))
            );
        }
    }

    private IEnumerable<string> GenerateInterfacePropertyMembers(LocalizationTreeNode node)
    {
        return node.Children.Select(x =>
        {
            var identifierKey = x.GetFullIdentifierKey("_");
            if (x.Type == LocalizationTreeNodeType.Leaf)
            {
                var typeName = x.Item.ValueArgumentTypes.Length is 0
                    ? "LocalizedString"
                    : $"LocalizedString<{string.Join(", ", x.Item.ValueArgumentTypes)}>";
                return $$"""
                    /// <summary>
                    /// {{ConvertValueToComment(x.Item.SampleValue)}}
                    /// </summary>
                    {{typeName}} {{x.IdentifierKey}} { get; }
                    """;
            }
            else
            {
                return $$"""
                    /// <summary>
                    /// 获取 {{x.IdentifierKey}} 分组的本地化字符串。
                    /// </summary>
                    ILocalizedValues_{{identifierKey}} {{x.IdentifierKey}} { get; }
                    """;
            }
        });
    }

    private IEnumerable<LocalizationTreeNode> EnumerateAllNonLeafDescendants(LocalizationTreeNode root)
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

    private string ConvertValueToComment(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "";
        }

        var escaped = value!
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

        if (!escaped.Contains('\n'))
        {
            return escaped;
        }

        var lines = escaped.Replace("\r\n", "\n").Split('\n');
        return string.Join("<br/>\n    /// ", lines);
    }

    #endregion

    #region Language Value Implementations (Dictionary)

    public string ToDictionaryImmutableValuesCodeText(LocalizationGeneratingModel model)
    {
        var isNestedSource = model.DependencyMode == DependencyMode.NestedSource;
        using var builder = isNestedSource
            ? new SourceTextBuilder(model.Namespace)
            : new SourceTextBuilder(GeneratorInfo.RootNamespace);
        if (!isNestedSource)
        {
            builder
                .UsingTypeAlias("ILocalizedStringProvider", "DotNetCampus.Localizations.ILocalizedStringProvider")
                .UsingTypeAlias("LocalizedString", "DotNetCampus.Localizations.LocalizedString");
        }

        Action<IAllowTypeDeclaration> addTypes = target =>
        {
            AddImmutableValuesDeclarations(target, model.TypeName, Tree);
        };

        if (isNestedSource)
        {
            builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper => addTypes(wrapper));
        }
        else
        {
            addTypes(builder);
        }

        return builder.ToString();
    }

    public string ToDictionaryNotifiableValuesCodeText(LocalizationGeneratingModel model)
    {
        var isNestedSource = model.DependencyMode == DependencyMode.NestedSource;
        var accessibility = isNestedSource ? "public" : model.TypeAccessibility;
        using var builder = isNestedSource
            ? new SourceTextBuilder(model.Namespace) { RemoveIndentForPreprocessorLines = true }
            : new SourceTextBuilder(GeneratorInfo.RootNamespace) { RemoveIndentForPreprocessorLines = true };
        builder
            .UsingTypeAlias("INotifyPropertyChanged", "System.ComponentModel.INotifyPropertyChanged")
            .UsingTypeAlias("PropertyChangedEventArgs", "System.ComponentModel.PropertyChangedEventArgs")
            .UsingTypeAlias("PropertyChangedEventHandler", "System.ComponentModel.PropertyChangedEventHandler");
        if (!isNestedSource)
        {
            builder
                .UsingTypeAlias("ILocalizedStringProvider", "DotNetCampus.Localizations.ILocalizedStringProvider")
                .UsingTypeAlias("LocalizedString", "DotNetCampus.Localizations.LocalizedString");
        }

        Action<IAllowTypeDeclaration> addTypes = target =>
        {
            AddNotifiableValuesDeclarations(target, model.TypeName, accessibility, Tree);
        };

        if (isNestedSource)
        {
            builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper => addTypes(wrapper));
        }
        else
        {
            addTypes(builder);
        }

        return builder.ToString();
    }

    private void AddImmutableValuesDeclarations(IAllowTypeDeclaration target, string typeName, LocalizationTreeNode root)
    {
        // Root class
        target.AddTypeDeclaration("internal sealed class ImmutableLocalizedValues(ILocalizedStringProvider provider)", t => t
            .AddGeneratedToolAndEditorBrowsingAttributes()
            .AddAttribute("[global::System.Diagnostics.DebuggerDisplay(\"[{LocalizedStringProvider.IetfLanguageTag}] " + typeName + ".???\")]")
            .AddBaseTypes("ILocalizedValues")
            .AddRawMembers(
                "public ILocalizedStringProvider LocalizedStringProvider => provider;",
                "public string IetfLanguageTag => provider.IetfLanguageTag;",
                "public string this[string key] => provider[key];")
            .AddRawMembers(GenerateImmutablePropertyMembers(root, "Immutable"))
        );

        // Child classes
        foreach (var node in EnumerateAllNonLeafDescendants(root))
        {
            var nodeTypeName = node.GetFullIdentifierKey("_");
            var nodeKeyName = node.GetFullIdentifierKey(".");
            target.AddTypeDeclaration($"internal sealed class ImmutableLocalizedValues_{nodeTypeName}(ILocalizedStringProvider provider)", t => t
                .AddGeneratedToolAndEditorBrowsingAttributes()
                .AddAttribute("[global::System.Diagnostics.DebuggerDisplay(\"[{LocalizedStringProvider.IetfLanguageTag}] " + typeName + "." + nodeKeyName + ".???\")]")
                .AddBaseTypes($"ILocalizedValues_{nodeTypeName}")
                .AddRawMembers("public ILocalizedStringProvider LocalizedStringProvider => provider;")
                .AddRawMembers(GenerateImmutablePropertyMembers(node, "Immutable"))
            );
        }
    }

    private void AddNotifiableValuesDeclarations(IAllowTypeDeclaration target, string typeName, string accessibility, LocalizationTreeNode root)
    {
        // Root class
        target.AddTypeDeclaration($"{accessibility} sealed class NotifiableLocalizedValues", t => t
            .WithSummaryComment("提供可通知属性变更的本地化字符串集，当语言文化切换时会发出属性变更通知。")
            .AddGeneratedToolAndEditorBrowsingAttributes()
            .AddAttribute("[global::System.Diagnostics.DebuggerDisplay(\"[{LocalizedStringProvider.IetfLanguageTag}] " + typeName + ".???\")]")
            .AddBaseTypes("ILocalizedValues", "INotifyPropertyChanged")
            .AddRawMembers(
                "public ILocalizedStringProvider LocalizedStringProvider { get; private set; }",
                GenerateNotifiableConstructor("NotifiableLocalizedValues", root),
                "public string IetfLanguageTag => LocalizedStringProvider.IetfLanguageTag;",
                "public string this[string key] => LocalizedStringProvider[key];")
            .AddRawMembers(GenerateNotifiablePropertyMembers(root))
            .AddRawMembers(
                GenerateSetProviderMethod(root),
                """
                #pragma warning disable CS0067
                public event PropertyChangedEventHandler? PropertyChanged;
                #pragma warning restore CS0067
                """)
        );

        // Child classes
        foreach (var node in EnumerateAllNonLeafDescendants(root))
        {
            var nodeTypeName = node.GetFullIdentifierKey("_");
            var nodeKeyName = node.GetFullIdentifierKey(".");
            target.AddTypeDeclaration($"internal sealed class NotifiableLocalizedValues_{nodeTypeName}", t => t
                .AddGeneratedToolAndEditorBrowsingAttributes()
                .AddAttribute("[global::System.Diagnostics.DebuggerDisplay(\"[{LocalizedStringProvider.IetfLanguageTag}] " + typeName + "." + nodeKeyName + ".???\")]")
                .AddBaseTypes($"ILocalizedValues_{nodeTypeName}", "INotifyPropertyChanged")
                .AddRawMembers(
                    "public ILocalizedStringProvider LocalizedStringProvider { get; private set; }",
                    GenerateNotifiableConstructor($"NotifiableLocalizedValues_{nodeTypeName}", node))
                .AddRawMembers(GenerateNotifiablePropertyMembers(node))
                .AddRawMembers(
                    GenerateSetProviderMethod(node),
                    """
                    #pragma warning disable CS0067
                    public event PropertyChangedEventHandler? PropertyChanged;
                    #pragma warning restore CS0067
                    """)
            );
        }
    }

    private IEnumerable<string> GenerateImmutablePropertyMembers(LocalizationTreeNode node, string typePrefix)
    {
        return node.Children.Select(x =>
        {
            var identifierKey = x.GetFullIdentifierKey("_");
            if (x.Type == LocalizationTreeNodeType.Leaf)
            {
                return x.Item.ValueArgumentTypes.Length is 0
                    ? $"""public LocalizedString {x.IdentifierKey} => provider.Get0("{x.Item.Key}");"""
                    : $"""public LocalizedString<{string.Join(", ", x.Item.ValueArgumentTypes)}> {x.IdentifierKey} => provider.Get{x.Item.ValueArgumentTypes.Length}<{string.Join(", ", x.Item.ValueArgumentTypes)}>("{x.Item.Key}");""";
            }
            else
            {
                return $"public ILocalizedValues_{identifierKey} {x.IdentifierKey} {{ get; }} = new {typePrefix}LocalizedValues_{identifierKey}(provider);";
            }
        });
    }

    private string GenerateNotifiableConstructor(string className, LocalizationTreeNode node)
    {
        var navChildren = node.Children.Where(x => x.Type == LocalizationTreeNodeType.Category).ToList();
        if (navChildren.Count == 0)
        {
            return $$"""
                public {{className}}(ILocalizedStringProvider provider)
                {
                    LocalizedStringProvider = provider;
                }
                """;
        }

        var assignments = navChildren.Select(x =>
            $"    {x.IdentifierKey} = new NotifiableLocalizedValues_{x.GetFullIdentifierKey("_")}(provider);");
        return $$"""
            public {{className}}(ILocalizedStringProvider provider)
            {
                LocalizedStringProvider = provider;
            {{string.Join("\n", assignments)}}
            }
            """;
    }

    private IEnumerable<string> GenerateNotifiablePropertyMembers(LocalizationTreeNode node)
    {
        return node.Children.Select(x =>
        {
            var identifierKey = x.GetFullIdentifierKey("_");
            if (x.Type == LocalizationTreeNodeType.Leaf)
            {
                return x.Item.ValueArgumentTypes.Length is 0
                    ? $"""public LocalizedString {x.IdentifierKey} => LocalizedStringProvider.Get0("{x.Item.Key}");"""
                    : $"""public LocalizedString<{string.Join(", ", x.Item.ValueArgumentTypes)}> {x.IdentifierKey} => LocalizedStringProvider.Get{x.Item.ValueArgumentTypes.Length}<{string.Join(", ", x.Item.ValueArgumentTypes)}>("{x.Item.Key}");""";
            }
            else
            {
                return $"public ILocalizedValues_{identifierKey} {x.IdentifierKey} {{ get; }}";
            }
        });
    }

    private string GenerateSetProviderMethod(LocalizationTreeNode node)
    {
        var lines = new List<string> { "LocalizedStringProvider = newProvider;" };
        foreach (var child in node.Children)
        {
            if (child.Type == LocalizationTreeNodeType.Leaf)
            {
                lines.Add($"""PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("{child.IdentifierKey}"));""");
            }
            else
            {
                lines.Add($"((NotifiableLocalizedValues_{child.GetFullIdentifierKey("_")}){child.IdentifierKey}).SetProvider(newProvider);");
            }
        }

        return $$"""
            internal void SetProvider(ILocalizedStringProvider newProvider)
            {
            {{string.Join("\n", lines.Select(l => $"    {l}"))}}
            }
            """;
    }

    #endregion

    #region Language Value Implementations (Compiled)

    public string ToCompiledValuesCodeText(LocalizationGeneratingModel model, string ietfLanguageTag, LocalizationCodeTransformer referenceTransformer)
    {
        var isNestedSource = model.DependencyMode == DependencyMode.NestedSource;
        using var builder = isNestedSource
            ? new SourceTextBuilder(model.Namespace)
            : new SourceTextBuilder(GeneratorInfo.RootNamespace);
        if (!isNestedSource)
        {
            builder.Using("DotNetCampus.Localizations");
        }

        var tagIdentifier = IetfLanguageTagToIdentifier(ietfLanguageTag);
        var allInterfaces = new List<string> { "ILocalizedValues" };
        allInterfaces.AddRange(referenceTransformer.EnumerateAllNonLeafDescendants(referenceTransformer.Tree)
            .Select(n => $"ILocalizedValues_{n.GetFullIdentifierKey("_")}"));

        Action<IAllowTypeDeclaration> addType = target =>
        {
            target.AddTypeDeclaration($"internal sealed class LocalizedValues_{tagIdentifier}", t =>
            {
                t.AddGeneratedToolAndEditorBrowsingAttributes();
                t.AddAttribute($"""[global::System.Diagnostics.DebuggerDisplay("[{ietfLanguageTag}]")]""");
                t.AddBaseTypes(allInterfaces.ToArray());
                t.AddRawMembers($"public static LocalizedValues_{tagIdentifier} Instance {{ get; }} = new();");
                if (!isNestedSource)
                {
                    // Library 模式下 ILocalizedValues : ILocalizedStringProvider，需要实现 IetfLanguageTag 和 indexer。
                    // Compiled 模式下无法高效实现索引器（所有 key 都是字面量属性，没有字典），抛异常以明确告知误用。
                    t.AddRawMembers(
                        $"""public string IetfLanguageTag => "{ietfLanguageTag}";""",
                        """public string this[string key] => throw new global::System.NotSupportedException("Compiled 模式不支持基于字符串 key 的动态索引，请使用类型化属性。");""");
                }
                t.AddRawMembers(GenerateCompiledExplicitMembers(referenceTransformer.Tree, this));
            });
        };

        if (isNestedSource)
        {
            builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper => addType(wrapper));
        }
        else
        {
            addType(builder);
        }

        return builder.ToString();
    }

    public string ToCompiledNotifiableValuesCodeText(LocalizationGeneratingModel model)
    {
        var isNestedSource = model.DependencyMode == DependencyMode.NestedSource;
        var accessibility = isNestedSource ? "public" : model.TypeAccessibility;
        using var builder = isNestedSource
            ? new SourceTextBuilder(model.Namespace) { RemoveIndentForPreprocessorLines = true }
            : new SourceTextBuilder(GeneratorInfo.RootNamespace) { RemoveIndentForPreprocessorLines = true };
        builder
            .UsingTypeAlias("INotifyPropertyChanged", "System.ComponentModel.INotifyPropertyChanged")
            .UsingTypeAlias("PropertyChangedEventArgs", "System.ComponentModel.PropertyChangedEventArgs")
            .UsingTypeAlias("PropertyChangedEventHandler", "System.ComponentModel.PropertyChangedEventHandler");
        if (!isNestedSource)
        {
            builder
                .Using("DotNetCampus.Localizations")
                .UsingTypeAlias("ILocalizedStringProvider", "DotNetCampus.Localizations.ILocalizedStringProvider");
        }

        var allInterfaces = new List<string> { "ILocalizedValues" };
        allInterfaces.AddRange(EnumerateAllNonLeafDescendants(Tree)
            .Select(n => $"ILocalizedValues_{n.GetFullIdentifierKey("_")}"));
        allInterfaces.Add("INotifyPropertyChanged");

        // Collect all non-leaf descendants for _inner fields
        var nonLeafNodes = EnumerateAllNonLeafDescendants(Tree).ToList();

        Action<IAllowTypeDeclaration> addType = target =>
        {
            target.AddTypeDeclaration($"{accessibility} sealed class NotifiableLocalizedValues", t =>
            {
                t.WithSummaryComment("提供可通知属性变更的本地化字符串集，当语言文化切换时会发出属性变更通知。");
                t.AddGeneratedToolAndEditorBrowsingAttributes();
                if (!isNestedSource)
                {
                    t.AddAttribute("[global::System.Diagnostics.DebuggerDisplay(\"[{IetfLanguageTag}]\")]");
                }
                t.AddBaseTypes(allInterfaces.ToArray());
                t.AddRawMembers(GenerateNotifiableCompiledFields(nonLeafNodes));
                t.AddRawMembers(GenerateNotifiableCompiledConstructor(nonLeafNodes));
                if (!isNestedSource)
                {
                    t.AddRawMembers(
                        "public string IetfLanguageTag => (_inner as ILocalizedStringProvider)?.IetfLanguageTag ?? \"\";",
                        "public string this[string key] => (_inner as ILocalizedStringProvider)?[key] ?? \"\";");
                }
                t.AddRawMembers(GenerateCompiledExplicitMembersForNotifiable(Tree));
                t.AddRawMembers(
                    GenerateCompiledSetInnerMethod(nonLeafNodes),
                    """
                    #pragma warning disable CS0067
                    public event PropertyChangedEventHandler? PropertyChanged;
                    #pragma warning restore CS0067
                    """);
            });
        };

        if (isNestedSource)
        {
            builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper => addType(wrapper));
        }
        else
        {
            addType(builder);
        }

        return builder.ToString();
    }

    private IEnumerable<string> GenerateCompiledExplicitMembers(LocalizationTreeNode root, LocalizationCodeTransformer valueSource)
    {
        // Build a dictionary of key -> value from valueSource for quick lookup
        var valueMap = valueSource.LocalizationItems.ToDictionary(x => x.Key, x => x.Value);

        return GenerateExplicitMembersForNode(root, "ILocalizedValues", valueMap);
    }

    private IEnumerable<string> GenerateExplicitMembersForNode(LocalizationTreeNode node, string interfaceName, Dictionary<string, string> valueMap)
    {
        var members = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type == LocalizationTreeNodeType.Category)
            {
                // Navigation property → returns this
                var childInterfaceName = $"ILocalizedValues_{child.GetFullIdentifierKey("_")}";
                members.Add($"{childInterfaceName} {interfaceName}.{child.IdentifierKey} => this;");
            }
            else
            {
                // Leaf property → returns literal
                var escapedValue = SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(valueMap.TryGetValue(child.Item.Key, out var v) ? v : "")).ToFullString();
                if (child.Item.ValueArgumentTypes.Length is 0)
                {
                    members.Add($"""LocalizedString {interfaceName}.{child.IdentifierKey} => new("{child.Item.Key}", {escapedValue});""");
                }
                else
                {
                    var genericTypes = string.Join(", ", child.Item.ValueArgumentTypes);
                    members.Add($"""LocalizedString<{genericTypes}> {interfaceName}.{child.IdentifierKey} => new("{child.Item.Key}", {escapedValue});""");
                }
            }
        }

        // Recurse into non-leaf children
        foreach (var child in node.Children.Where(c => c.Type == LocalizationTreeNodeType.Category))
        {
            var childInterfaceName = $"ILocalizedValues_{child.GetFullIdentifierKey("_")}";
            members.AddRange(GenerateExplicitMembersForNode(child, childInterfaceName, valueMap));
        }

        return members;
    }

    private IEnumerable<string> GenerateNotifiableCompiledFields(List<LocalizationTreeNode> nonLeafNodes)
    {
        var fields = new List<string> { "private ILocalizedValues _inner;" };
        foreach (var node in nonLeafNodes)
        {
            var fieldName = "_inner_" + node.GetFullIdentifierKey("_");
            var interfaceName = $"ILocalizedValues_{node.GetFullIdentifierKey("_")}";
            fields.Add($"private {interfaceName} {fieldName};");
        }
        return fields;
    }

    private string GenerateNotifiableCompiledConstructor(List<LocalizationTreeNode> nonLeafNodes)
    {
        var assignments = new List<string> { "_inner = inner;" };
        foreach (var node in nonLeafNodes)
        {
            var fieldName = "_inner_" + node.GetFullIdentifierKey("_");
            var accessPath = "inner." + node.GetFullIdentifierKey(".");
            assignments.Add($"{fieldName} = {accessPath};");
        }

        return $$"""
            public NotifiableLocalizedValues(ILocalizedValues inner)
            {
            {{string.Join("\n", assignments.Select(a => $"    {a}"))}}
            }
            """;
    }

    private IEnumerable<string> GenerateCompiledExplicitMembersForNotifiable(LocalizationTreeNode root)
    {
        return GenerateNotifiableExplicitMembersForNode(root, "ILocalizedValues");
    }

    private IEnumerable<string> GenerateNotifiableExplicitMembersForNode(LocalizationTreeNode node, string interfaceName)
    {
        var members = new List<string>();
        var fieldName = node == Tree ? "_inner" : "_inner_" + node.GetFullIdentifierKey("_");

        foreach (var child in node.Children)
        {
            if (child.Type == LocalizationTreeNodeType.Category)
            {
                var childInterfaceName = $"ILocalizedValues_{child.GetFullIdentifierKey("_")}";
                members.Add($"{childInterfaceName} {interfaceName}.{child.IdentifierKey} => this;");
            }
            else
            {
                if (child.Item.ValueArgumentTypes.Length is 0)
                {
                    members.Add($"LocalizedString {interfaceName}.{child.IdentifierKey} => {fieldName}.{child.IdentifierKey};");
                }
                else
                {
                    var genericTypes = string.Join(", ", child.Item.ValueArgumentTypes);
                    members.Add($"LocalizedString<{genericTypes}> {interfaceName}.{child.IdentifierKey} => {fieldName}.{child.IdentifierKey};");
                }
            }
        }

        foreach (var child in node.Children.Where(c => c.Type == LocalizationTreeNodeType.Category))
        {
            var childInterfaceName = $"ILocalizedValues_{child.GetFullIdentifierKey("_")}";
            members.AddRange(GenerateNotifiableExplicitMembersForNode(child, childInterfaceName));
        }

        return members;
    }

    private string GenerateCompiledSetInnerMethod(List<LocalizationTreeNode> nonLeafNodes)
    {
        var lines = new List<string> { "_inner = newInner;" };
        foreach (var node in nonLeafNodes)
        {
            var fieldName = "_inner_" + node.GetFullIdentifierKey("_");
            var accessPath = "newInner." + node.GetFullIdentifierKey(".");
            lines.Add($"{fieldName} = {accessPath};");
        }

        var allLeafNames = EnumerateAllLeaves(Tree).Select(l => l.IdentifierKey).ToList();
        foreach (var leafName in allLeafNames)
        {
            lines.Add($"""PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("{leafName}"));""");
        }

        return $$"""
            internal void SetInner(ILocalizedValues newInner)
            {
            {{string.Join("\n", lines.Select(l => $"    {l}"))}}
            }
            """;
    }

    private IEnumerable<LocalizationTreeNode> EnumerateAllLeaves(LocalizationTreeNode root)
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

    #endregion


    #region Language Value Provider

    public string ToProviderCodeText(LocalizationGeneratingModel model, string ietfLanguageTag)
    {
        var isNestedSource = model.DependencyMode == DependencyMode.NestedSource;
        using var builder = isNestedSource
            ? new SourceTextBuilder(model.Namespace)
            : new SourceTextBuilder(GeneratorInfo.RootNamespace);

        var typeName = IetfLanguageTagToIdentifier(ietfLanguageTag);
        var dictionaryEntries = string.Join("\n", LocalizationItems.Select(x => ConvertKeyValueToValueCodeLine(x.Key, x.Value)));

        Action<TypeDeclarationSourceTextBuilder> buildProvider = t => t
            .AddGeneratedToolAndEditorBrowsingAttributes()
            .AddAttribute($"""[global::System.Diagnostics.DebuggerDisplay("[{ietfLanguageTag}]")]""")
            .AddBaseTypes("ILocalizedStringProvider")
            .AddRawMembers(
                $"""public string IetfLanguageTag => "{ietfLanguageTag}";""",
                $$"""
                public string this[string key]
                {
                    get
                    {
                        if (_strings.TryGetValue(key, out var value) && value != null)
                        {
                            return value;
                        }
                        if (fallback != null)
                        {
                            return fallback[key];
                        }
                        return "";
                    }
                }
                """,
                $$"""
                private readonly global::System.Collections.Generic.Dictionary<string, string> _strings = new global::System.Collections.Generic.Dictionary<string, string>
                {
                {{dictionaryEntries}}
                };
                """
            );

        if (isNestedSource)
        {
            builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper =>
            {
                wrapper.AddTypeDeclaration($"internal class LocalizedStringProvider_{typeName}(ILocalizedStringProvider? fallback)", buildProvider);
            });
        }
        else
        {
            builder.AddTypeDeclaration($"internal class LocalizedStringProvider_{typeName}(ILocalizedStringProvider? fallback)", buildProvider);
        }

        return builder.ToString();
    }

    private string ConvertKeyValueToValueCodeLine(string key, string value)
    {
        var escapedValue = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value)).ToFullString();
        return $"    {{ \"{key}\", {escapedValue} }},";
    }

    #endregion

    #region Helpers

    private enum LocalizationTreeNodeType
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
    private class LocalizationTreeNode(LocalizationItem item, string identifierKey, ImmutableArray<string> identifierKeyParts, bool isRoot = false)
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

    #endregion
}
