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
            .UsingTypeAlias("LocalizedString", "DotNetCampus.Localizations.LocalizedString");
        AddInterfaceDeclarations(builder, model, model.TypeAccessibility);
        return builder.ToString();
    }

    public string GenerateNested(LocalizationGeneratingModel model)
    {
        using var builder = new SourceTextBuilder(model.Namespace);
        builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper =>
        {
            AddInterfaceDeclarations(wrapper, model, "public");
        });
        return builder.ToString();
    }

    private void AddInterfaceDeclarations(IAllowTypeDeclaration builder, LocalizationGeneratingModel model, string accessibility)
    {
        // ILocalizedValues 仅承载 Lang.A.B.C 形式的强类型导航语法糖，不携带任何随生成模式变化的运行时能力。
        builder.AddTypeDeclaration($"{accessibility} partial interface ILocalizedValues", t => t
            .WithSummaryComment("提供本地化字符串的访问接口。通过属性导航访问各分组和叶子节点的本地化值。")
            .AddGeneratedToolAndEditorBrowsingAttributes()
            .AddRawMembers(GenerateInterfacePropertyMembers(transformer.Tree))
        );

        // 仅 Dictionary 模式提供基于运行时字符串 key 的动态索引能力；Compiled 模式此能力应在编译期即不可用。
        if (model.GenerationMode == GenerationMode.Dictionary)
        {
            builder.AddTypeDeclaration($"{accessibility} partial interface IDictionaryLocalizedValues : ILocalizedValues", t => t
                .WithSummaryComment("在强类型导航访问之外，额外支持通过运行时字符串 key 动态访问本地化字符串。仅 Dictionary 生成模式提供。")
                .AddGeneratedToolAndEditorBrowsingAttributes()
                .AddRawMembers(
                    """
                    /// <summary>
                    /// 获取指定键的本地化字符串。如果字符串包含占位符，则返回值会包含形如 "{0}" "{1}" 的占位符用于格式化。
                    /// </summary>
                    /// <param name="key">要获取的本地化字符串的键。</param>
                    string this[string key] { get; }
                    """,
                    """
                    /// <summary>
                    /// 获取符合 IETF 规范的当前语言标签。
                    /// </summary>
                    string IetfLanguageTag { get; }
                    """)
            );
        }

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
