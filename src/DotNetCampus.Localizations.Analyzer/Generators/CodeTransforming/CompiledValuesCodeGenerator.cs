using System.Collections.Generic;
using System.Linq;
using DotNetCampus.Localizations.Generators.Builders;
using DotNetCampus.Localizations.Generators.ModelProviding;
using Microsoft.CodeAnalysis.CSharp;
using static DotNetCampus.Localizations.Generators.ModelProviding.IetfLanguageTagExtensions;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

internal class CompiledValuesCodeGenerator(LocalizationCodeTransformer transformer)
{
    public string Generate(LocalizationGeneratingModel model, string ietfLanguageTag, LocalizationCodeTransformer referenceTransformer)
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

        System.Action<IAllowTypeDeclaration> addType = target =>
        {
            target.AddTypeDeclaration($"internal sealed class LocalizedValues_{tagIdentifier}", t =>
            {
                t.AddGeneratedToolAndEditorBrowsingAttributes();
                t.AddAttribute($"""[global::System.Diagnostics.DebuggerDisplay("[{ietfLanguageTag}]")]""");
                t.AddBaseTypes(allInterfaces.ToArray());
                t.AddRawMembers($"public static LocalizedValues_{tagIdentifier} Instance {{ get; }} = new();");
                if (!isNestedSource)
                {
                    t.AddRawMembers(
                        $"""public string IetfLanguageTag => "{ietfLanguageTag}";""",
                        """public string this[string key] => throw new global::System.NotSupportedException("Compiled 模式不支持基于字符串 key 的动态索引，请使用类型化属性。");""");
                }
                t.AddRawMembers(GenerateCompiledExplicitMembers(referenceTransformer.Tree, transformer));
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

    public string GenerateNotifiable(LocalizationGeneratingModel model)
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
        allInterfaces.AddRange(transformer.EnumerateAllNonLeafDescendants(transformer.Tree)
            .Select(n => $"ILocalizedValues_{n.GetFullIdentifierKey("_")}"));
        allInterfaces.Add("INotifyPropertyChanged");

        var nonLeafNodes = transformer.EnumerateAllNonLeafDescendants(transformer.Tree).ToList();

        System.Action<IAllowTypeDeclaration> addType = target =>
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
                t.AddRawMembers(GenerateCompiledExplicitMembersForNotifiable(transformer.Tree));
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
                var childInterfaceName = $"ILocalizedValues_{child.GetFullIdentifierKey("_")}";
                members.Add($"{childInterfaceName} {interfaceName}.{child.IdentifierKey} => this;");
            }
            else
            {
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
        var fieldName = node == transformer.Tree ? "_inner" : "_inner_" + node.GetFullIdentifierKey("_");

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

        var allLeafNames = transformer.EnumerateAllLeaves(transformer.Tree).Select(l => l.IdentifierKey).ToList();
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
}
