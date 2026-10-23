using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace DotNetCampus.Localizations.Generators.CodeTransforming;

/// <summary>
/// 读取单一层级 YAML 格式语言文件。
/// </summary>
/// <remarks>
/// 只读取根映射中的标量键值对，并使用与 TOML 相同的文案转换规则，保留数字占位符的索引与重复引用。
/// </remarks>
public class YamlLocalizationFileReader : ILocalizationFileReader
{
    /// <inheritdoc />
    /// <remarks>根节点不是映射时返回空集合；嵌套映射、序列等非标量键值对不在当前读取范围内。</remarks>
    public ImmutableArray<LocalizationItem> Read(string content)
    {
        List<LocalizationItem> keyValues = [];
        var yaml = new YamlStream();
        yaml.Load(new StringReader(content));
        if (yaml.Documents[0].RootNode is not YamlMappingNode node)
        {
            return [];
        }

        // 只将单层标量键值对交给 CreateItem，文件结构解析与占位符转换分别处理。
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
    /// <remarks>转换后的文案用于生成可格式化字符串，原始文案仍作为本地化项的示例值保留。</remarks>
    private static LocalizationItem CreateItem(string yamlKey, string yamlValue)
    {
        // 共用占位符转换器，使 YAML 与 TOML 在参数顺序、重复索引及类型检查上的行为保持一致。
        var (valueTypes, value) = LocalizationValueConverter.ConvertToCodeValue(yamlValue);
        return new LocalizationItem(yamlKey, value, yamlValue, valueTypes, null);
    }
}
