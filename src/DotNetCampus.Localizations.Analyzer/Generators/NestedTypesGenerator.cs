using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using DotNetCampus.Localizations.Generators.Builders;
using DotNetCampus.Localizations.Generators.ModelProviding;
using DotNetCampus.Localizations.IO;
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
public class NestedTypesGenerator
{
    private const int MaxGenericArity = 8;

    public void Register(IncrementalGeneratorInitializationContext context)
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
        try
        {
            ExecuteCore(context, values);
        }
        catch (Exception ex)
        {
            context.ReportUnknownError(ex.Message);
        }
    }

    private void ExecuteCore(
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

        context.AddSource($"{model.Namespace}.Localizations/{model.TypeName}.LocalizedString.g.cs",
            SourceText.From(GenerateLocalizedStringTypes(model), Encoding.UTF8));

        if (model.GenerationMode == GenerationMode.Dictionary)
        {
            context.AddSource($"{model.Namespace}.Localizations/{model.TypeName}.ILocalizedStringProvider.g.cs",
                SourceText.From(GenerateILocalizedStringProvider(model), Encoding.UTF8));
        }

        context.AddSource($"{model.Namespace}.Localizations/{model.TypeName}.LocalizationFallbackHelper.g.cs",
            SourceText.From(GenerateLocalizationFallbackHelper(model), Encoding.UTF8));
    }

    private string GenerateLocalizedStringTypes(LocalizationGeneratingModel model)
    {
        using var builder = new SourceTextBuilder(model.Namespace)
            .AddRawText("#pragma warning disable CS0809");

        builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper =>
        {
            // LocalizedString（无参）
            wrapper.AddTypeDeclaration("public readonly record struct LocalizedString", t => t
                .WithSummaryComment("表示一个本地化字符串，可隐式转换为字符串。")
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

                wrapper.AddTypeDeclaration($"public readonly record struct LocalizedString<{typeParams}>", t => t
                    .WithSummaryComment($"表示一个带有 {arity} 个格式化参数的本地化字符串。")
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

    private string GenerateILocalizedStringProvider(LocalizationGeneratingModel model)
    {
        using var builder = new SourceTextBuilder(model.Namespace);

        builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper =>
        {
            wrapper.AddTypeDeclaration("public interface ILocalizedStringProvider", t =>
            {
                t.WithSummaryComment("为源生成器生成的本地化字符串提供统一的访问接口。");
                t.AddRawMembers(
                    "string IetfLanguageTag { get; }",
                    "string this[string key] { get; }");
                // Default interface methods to replace extension methods (can't use extensions in nested classes)
                t.AddRawMembers(
                    """LocalizedString Get0(string key) => new LocalizedString(key, this[key]);""");
                for (var arity = 1; arity <= MaxGenericArity; arity++)
                {
                    var typeParams = string.Join(", ", Enumerable.Range(1, arity).Select(i => $"T{i}"));
                    t.AddRawMembers(
                        $"LocalizedString<{typeParams}> Get{arity}<{typeParams}>(string key) => new LocalizedString<{typeParams}>(key, this[key]);");
                }
            });
        });

        return builder.ToString();
    }

    private string GenerateLocalizationFallbackHelper(LocalizationGeneratingModel model)
    {
        var template = EmbeddedSourceFile.Get("Assets/Helpers/LocalizationFallbackProvider.g.cs");
        var classBody = ExtractClassBody(template.Content);

        using var builder = new SourceTextBuilder(model.Namespace);
        builder
            .Using("System")
            .Using("System.Collections.Generic")
            .Using("System.Globalization")
            .Using("System.Linq");
        builder.AddTypeDeclaration($"partial class {model.TypeName}", wrapper =>
        {
            wrapper.AddTypeDeclaration("private static class LocalizationFallbackHelper", type =>
            {
                type.AddRawMembers(classBody);
            });
        });
        return builder.ToString();
    }

    private static string ExtractClassBody(string sourceText)
    {
        var lines = sourceText.Replace("\r\n", "\n").Split('\n');
        var startIndex = -1;
        var braceDepth = 0;
        var bodyLines = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            if (startIndex < 0)
            {
                if (lines[i].Contains("class ") && lines[i + 1].Trim() == "{")
                {
                    startIndex = i + 2;
                    braceDepth = 1;
                    i++;
                    continue;
                }
                if (lines[i].Contains("class ") && lines[i].TrimEnd().EndsWith("{"))
                {
                    startIndex = i + 1;
                    braceDepth = 1;
                    continue;
                }
            }
            else
            {
                foreach (var ch in lines[i])
                {
                    if (ch == '{') braceDepth++;
                    else if (ch == '}') braceDepth--;
                }

                if (braceDepth <= 0)
                {
                    break;
                }

                bodyLines.Add(lines[i]);
            }
        }

        return string.Join("\n", bodyLines);
    }
}
