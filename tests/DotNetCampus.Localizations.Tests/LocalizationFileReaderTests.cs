using DotNetCampus.Localizations.Generators.CodeTransforming;

namespace DotNetCampus.Localizations.Tests;

/// <summary>
/// 验证 TOML、YAML 读取器在重复占位符的类型校验、数字索引边界及参数槽位补齐上的一致行为。
/// </summary>
[TestClass]
public class LocalizationFileReaderTests
{
    /// <summary>
    /// 同一数字索引声明不同类型时必须报错，显式 object 与其他类型的先后顺序不影响校验结果。
    /// </summary>
    /// <param name="format">要验证的本地化文件格式。</param>
    /// <param name="value">包含同一索引的两个冲突类型声明的文案。</param>
    [TestMethod]
    [DataRow("toml", "{0:object} {0:int}")]
    [DataRow("toml", "{0:int} {0:object}")]
    [DataRow("yaml", "{0:object} {0:int}")]
    [DataRow("yaml", "{0:int} {0:object}")]
    public void WhenRepeatedIndexHasConflictingExplicitTypesThenReaderRejectsIt(string format, string value)
    {
        Assert.ThrowsExactly<FormatException>(() => Read(format, value));
    }

    /// <summary>
    /// 数字索引即使能够解析为 int，也不能超过本地化字符串支持的最大参数索引。
    /// </summary>
    /// <param name="format">要验证的本地化文件格式。</param>
    /// <param name="value">包含不受支持的数字索引的文案。</param>
    [TestMethod]
    [DataRow("toml", "{8}")]
    [DataRow("toml", "{2147483647}")]
    [DataRow("yaml", "{8}")]
    [DataRow("yaml", "{2147483647}")]
    public void WhenIndexExceedsSupportedArityThenReaderRejectsIt(string format, string value)
    {
        Assert.ThrowsExactly<NotSupportedException>(() => Read(format, value));
    }

    /// <summary>
    /// 超出 int 范围的数字必须按索引解析失败，不能退回命名占位符规则并被重新编号。
    /// </summary>
    /// <param name="format">要验证的本地化文件格式。</param>
    /// <param name="value">包含溢出数字索引的文案。</param>
    [TestMethod]
    [DataRow("toml", "{2147483648}")]
    [DataRow("toml", "{99999999999999999999}")]
    [DataRow("yaml", "{2147483648}")]
    [DataRow("yaml", "{99999999999999999999}")]
    public void WhenNumericIndexOverflowsThenReaderRejectsIt(string format, string value)
    {
        Assert.ThrowsExactly<OverflowException>(() => Read(format, value));
    }

    /// <summary>
    /// 最大合法索引对应第八个参数；即使前七个索引未出现，也必须保留这些参数槽位。
    /// </summary>
    /// <param name="format">要验证的本地化文件格式。</param>
    [TestMethod]
    [DataRow("toml")]
    [DataRow("yaml")]
    public void WhenHighestSupportedIndexIsUsedThenReaderPreservesAllArgumentSlots(string format)
    {
        var item = Read(format, "{7}");

        // 同时检查生成方法所需的参数数量与实际取值，防止只保留文本索引而没有补齐类型数组。
        Assert.AreEqual(8, item.ValueArgumentTypes.Length);
        Assert.AreEqual("49", string.Format(item.Value, 0, 1, 2, 3, 4, 5, 6, 49));
    }

    /// <summary>
    /// 将测试文案包装为单个本地化条目，并通过对应格式的真实读取器进行解析。
    /// </summary>
    /// <param name="format">测试数据指定的文件格式，取值为 toml 或 yaml。</param>
    /// <param name="value">当前用例中的占位符文案。</param>
    /// <returns>真实读取器返回的唯一一个本地化项。</returns>
    private static LocalizationItem Read(string format, string value)
    {
        ILocalizationFileReader reader = format == "toml"
            ? new TomlLocalizationFileReader()
            : new YamlLocalizationFileReader();

        // 用最小文件内容经过完整的文件解析和占位符转换流程，避免测试替代实现。
        var content = format == "toml" ? $"Text = \"{value}\"" : $"Text: \"{value}\"";
        return reader.Read(content)[0];
    }
}
