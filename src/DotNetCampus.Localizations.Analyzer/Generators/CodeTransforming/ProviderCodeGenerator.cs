using System.Collections.Generic;
using System.Linq;
using DotNetCampus.Localizations.Generators.Builders;
using DotNetCampus.Localizations.Generators.ModelProviding;
using Microsoft.CodeAnalysis.CSharp;
using static DotNetCampus.Localizations.Generators.ModelProviding.IetfLanguageTagExtensions;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

internal class ProviderCodeGenerator(LocalizationCodeTransformer transformer)
{
    public string Generate(LocalizationGeneratingModel model, string ietfLanguageTag)
    {
        var isNestedSource = model.DependencyMode == DependencyMode.NestedSource;
        using var builder = isNestedSource
            ? new SourceTextBuilder(model.Namespace)
            : new SourceTextBuilder(GeneratorInfo.RootNamespace);

        var typeName = IetfLanguageTagToIdentifier(ietfLanguageTag);
        var dictionaryEntries = string.Join("\n", transformer.LocalizationItems.Select(x => ConvertKeyValueToValueCodeLine(x.Key, x.Value)));
        // 比较规则必须写入生成代码，使运行时查询及回退与生成阶段的 Key 校验一致。
        var comparer = model.IsKeysCaseSensitive ? "Ordinal" : "OrdinalIgnoreCase";

        System.Action<TypeDeclarationSourceTextBuilder> buildProvider = t => t
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
                private readonly global::System.Collections.Generic.Dictionary<string, string> _strings = new global::System.Collections.Generic.Dictionary<string, string>(global::System.StringComparer.{{comparer}})
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

    private static string ConvertKeyValueToValueCodeLine(string key, string value)
    {
        var escapedValue = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value)).ToFullString();
        return $"    {{ \"{key}\", {escapedValue} }},";
    }
}
