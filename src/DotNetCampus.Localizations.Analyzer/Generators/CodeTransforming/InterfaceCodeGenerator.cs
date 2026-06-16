using System.Collections.Generic;
using System.Linq;
using DotNetCampus.Localizations.Generators.Builders;
using DotNetCampus.Localizations.Generators.ModelProviding;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

internal class InterfaceCodeGenerator(LocalizationCodeTransformer transformer)
{
    public string Generate(LocalizationGeneratingModel model)
    {
        using var builder = new SourceTextBuilder(GeneratorInfo.RootNamespace);
        builder
            .Using("DotNetCampus.Localizations")
            .UsingTypeAlias("ILocalizedStringProvider", "DotNetCampus.Localizations.ILocalizedStringProvider")
            .UsingTypeAlias("LocalizedString", "DotNetCampus.Localizations.LocalizedString");
        AddInterfaceDeclarations(builder, model.TypeAccessibility, includeBaseInterface: true);
        return builder.ToString();
    }

    public string GenerateNested(LocalizationGeneratingModel model)
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
            .AddRawMembers(GenerateInterfacePropertyMembers(transformer.Tree))
        );

        foreach (var node in transformer.EnumerateAllNonLeafDescendants(transformer.Tree))
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
                    /// {{transformer.ConvertValueToComment(x.Item.SampleValue)}}
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
}
