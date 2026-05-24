using System;
using System.Collections.Generic;
using System.Linq;
using DotNetCampus.Localizations.Generators.Builders;
using DotNetCampus.Localizations.Generators.ModelProviding;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

internal class DictionaryValuesCodeGenerator(LocalizationCodeTransformer transformer)
{
    public string GenerateImmutable(LocalizationGeneratingModel model)
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
            AddImmutableValuesDeclarations(target, model.TypeName, transformer.Tree);
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
                .UsingTypeAlias("ILocalizedStringProvider", "DotNetCampus.Localizations.ILocalizedStringProvider")
                .UsingTypeAlias("LocalizedString", "DotNetCampus.Localizations.LocalizedString");
        }

        Action<IAllowTypeDeclaration> addTypes = target =>
        {
            AddNotifiableValuesDeclarations(target, model.TypeName, accessibility, transformer.Tree);
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

        foreach (var node in transformer.EnumerateAllNonLeafDescendants(root))
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

        foreach (var node in transformer.EnumerateAllNonLeafDescendants(root))
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
}
