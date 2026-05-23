using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DotNetCampus.Localizations.Generators.Builders;
using DotNetCampus.Localizations.Generators.CodeTransforming;
using DotNetCampus.Localizations.Generators.ModelProviding;
using DotNetCampus.Localizations.Utils.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DotNetCampus.Localizations.Generators;

/// <summary>
/// 当 <see cref="DependencyMode.NestedSource"/> 时，生成 <c>LocalizedString</c> 等基础结构体作为内部类型，
/// 使项目无需引用 <c>DotNetCampus.Localizations.dll</c>。
/// </summary>
/// <remarks>
/// <para>输出文件：<c>{TypeName}.LocalizedString.g.cs</c></para>
/// <para>触发条件：<see cref="DependencyMode.NestedSource"/>。</para>
/// </remarks>
[Generator]
public class NestedTypesGenerator : IIncrementalGenerator
{
    private const int MaxGenericArity = 4;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var localizationTypeProvider = context.SyntaxProvider.SelectGeneratingModels();
        var globalOptionsProvider = context.AnalyzerConfigOptionsProvider;
        context.RegisterSourceOutput(
            localizationTypeProvider.Combine(globalOptionsProvider),
            Execute);
    }

    private void Execute(
        SourceProductionContext context,
        (LocalizationGeneratingModel Left, AnalyzerConfigOptionsProvider Right) values)
    {
        var (model, options) = values;

        if (model.DependencyMode != DependencyMode.NestedSource)
        {
            return;
        }

        var isIncludedByPackageReference = options.GlobalOptions.GetBoolean("LocalizationIsIncludedByPackageReference");
        if (!isIncludedByPackageReference)
        {
            return;
        }

        var code = GenerateLocalizedStringTypes(model);
        context.AddSource($"{model.TypeName}.LocalizedString.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private string GenerateLocalizedStringTypes(LocalizationGeneratingModel model)
    {
        using var builder = new SourceTextBuilder(model.Namespace);

        builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper =>
        {
            // LocalizedString（无参）
            wrapper.AddTypeDeclaration("internal readonly record struct LocalizedString", t => t
                .AddRawMembers(
                    "private readonly string _key;",
                    """
                    public LocalizedString(string key, string value)
                    {
                        _key = key;
                        Value = value;
                    }
                    """,
                    "public string Value { get; }",
                    "public static implicit operator string(LocalizedString localizedString) => localizedString.Value;",
                    "public override string ToString() => Value;")
            );

            // LocalizedString<T1> ... LocalizedString<T1, T2, T3, T4>
            for (var arity = 1; arity <= MaxGenericArity; arity++)
            {
                var typeParams = string.Join(", ", Enumerable.Range(1, arity).Select(i => $"T{i}"));
                var methodParams = string.Join(", ", Enumerable.Range(1, arity).Select(i => $"T{i} arg{i}"));
                var formatArgs = string.Join(", ", Enumerable.Range(1, arity).Select(i => $"arg{i}"));

                wrapper.AddTypeDeclaration($"internal readonly record struct LocalizedString<{typeParams}>", t => t
                    .AddRawMembers(
                        "private readonly string _key;",
                        "private readonly string _value;",
                        """
                        public LocalizedString(string key, string value)
                        {
                            _key = key;
                            _value = value;
                        }
                        """,
                        $"public string ToString({methodParams}) => string.Format(_value, {formatArgs});",
                        """
                        [global::System.Obsolete("请使用带参数的 ToString 方法。", true)]
                        public override string ToString() => _value;
                        """)
                );
            }
        });

        return builder.ToString();
    }
}
