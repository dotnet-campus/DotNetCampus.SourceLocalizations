using System.Collections.Generic;
using System.Globalization;
using System.Security;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace DotNetCampus.Localizations.Generators;

[Generator]
public class IetfLanguageTagsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(Execute);
    }

    private void Execute(IncrementalGeneratorPostInitializationContext context)
    {
        var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

        var ietfLanguageTagsCode = GenerateIetfLanguageTagsCode(allCultures);
        context.AddSource("IetfLanguageTags.const.g.cs", SourceText.From(ietfLanguageTagsCode, Encoding.UTF8));

        var ietfLanguageTagDictionaryCode = GenerateIetfLanguageTagDictionaryCode(allCultures);
        context.AddSource("IetfLanguageTags.hashset.g.cs", SourceText.From(ietfLanguageTagDictionaryCode, Encoding.UTF8));
    }

    private string GenerateIetfLanguageTagsCode(CultureInfo[] allCultures) => $$"""
namespace DotNetCampus.Localizations.Ietf;

partial class IetfLanguageTags
{
{{string.Join("\n\n", GenerateConstantTagProperties(allCultures))}}
}

""";

    private string GenerateIetfLanguageTagDictionaryCode(CultureInfo[] allCultures) => $$"""
namespace DotNetCampus.Localizations.Ietf;

partial class IetfLanguageTags
{
    /// <summary>
    /// 包含所有 IETF 语言标签字符串常量的不可变哈希集合。
    /// </summary>
    public static global::System.Collections.Generic.HashSet<string> Set { get; } = 
    [
{{string.Join("\n", GenerateDictionaryTagKeyValues(allCultures))}}
    ];
}

""";

    /// <summary>
    /// <list type="bullet">
    /// <item></item>
    /// </list>
    /// </summary>
    /// <param name="allCultures"></param>
    /// <returns></returns>
    private IEnumerable<string> GenerateConstantTagProperties(CultureInfo[] allCultures)
    {
        foreach (var culture in allCultures)
        {
            var name = culture.Name;
            if (name is "")
            {
                // culture.ThreeLetterISOLanguageName = ivl
                continue;
            }

            var identifier = name switch
            {
                "as" => "@as",
                "is" => "@is",
                _ => name.Replace('-', '_'),
            };

            yield return $"""
    /// <summary>
    /// {SecurityElement.Escape(culture.EnglishName)}
    /// <list type="bullet">
    /// <item>{SecurityElement.Escape(culture.NativeName)}</item>
    /// <item>{SecurityElement.Escape(culture.DisplayName)}</item>
    /// </list>
    /// </summary>
    public const string {identifier} = "{name}";
""";
        }
    }

    private IEnumerable<string> GenerateDictionaryTagKeyValues(CultureInfo[] allCultures)
    {
        foreach (var culture in allCultures)
        {
            var name = culture.Name;
            if (name is "")
            {
                // culture.ThreeLetterISOLanguageName = ivl
                continue;
            }

            yield return $"""
        "{name}",
""";
        }
    }
}
