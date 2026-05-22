using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

/// <summary>
/// 读取单一层级 YAML 格式语言文件。
/// </summary>
public class YamlLocalizationFileReader : ILocalizationFileReader
{
    /// <inheritdoc />
    public ImmutableArray<LocalizationItem> Read(string content)
    {
        List<LocalizationItem> keyValues = [];
        var yaml = new YamlStream();
        yaml.Load(new StringReader(content));
        if (yaml.Documents[0].RootNode is not YamlMappingNode node)
        {
            return [];
        }

        keyValues.AddRange(node.Children
            .Where(x => x is { Key: YamlScalarNode, Value: YamlScalarNode })
            .Select(x => CreateItem(
                ((YamlScalarNode)x.Key).Value!,
                ((YamlScalarNode)x.Value).Value!)));

        return [..keyValues];
    }

    /// <summary>
    /// 从 YAML 中读取到的键值对创建本地化项。
    /// </summary>
    /// <param name="yamlKey">YAML 文件中的键。</param>
    /// <param name="yamlValue">YAML 文件中的值。</param>
    /// <returns>本地化项。</returns>
    private static LocalizationItem CreateItem(string yamlKey, string yamlValue)
    {
        var (valueTypes, value) = ConvertYamlValueToCodeValue(yamlValue);
        return new LocalizationItem(yamlKey, value, yamlValue, valueTypes, null);
    }

    /// <summary>
    /// 将 YAML 文件中的语言项值转换为适用于 C# 代码中可格式化字符串的值。
    /// </summary>
    /// <example>
    /// 从 YAML 文件中读取的语言项值为 <c>"{name:string} is {age:int} years old."</c>，转换为 <c>([string, int],"{0} is {1} years old.")</c>。
    /// </example>
    /// <param name="value">YAML 文件中的语言项值。</param>
    /// <returns>适用于 C# 代码中可格式化字符串的值。</returns>
    private static (ImmutableArray<string> Types, string Value) ConvertYamlValueToCodeValue(string value)
    {
        var regex = new Regex(@"\{(?<name>[^{}:]+)(?::(?<type>[^{}:]+))?\}");
        var matches = regex.Matches(value);
        ImmutableArray<string> types = matches.Count is 0
            ? []
            : [..matches.OfType<Match>().Select(x => x.Groups["type"].Success ? x.Groups["type"].Value : "object")];
        var index = 0;
        var v = regex.Replace(value, _ => $"{{{index++}}}");
        return (types, v);
    }
}
