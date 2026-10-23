using DotNetCampus.Localizations;

namespace PlaceholderIndices;

/// <summary>
/// 为数字占位符回归用例生成本地化入口，供测试程序访问 TOML、YAML 两组资源。
/// </summary>
/// <remarks>
/// 使用英文默认语言推导参数签名，以验证乱序的类型标注仍按数字索引排列；
/// 运行时先读取中文，再切换为英文，确保同一调用约定适用于两种语言。
/// </remarks>
[LocalizedConfiguration(
    Default = "en",
    Current = "zh-Hans",
    EnsureKeysIdentical = true,
    GenerationMode = GenerationMode.Dictionary,
    NotificationMode = NotificationMode.CurrentCulturePropertyChanged)]
internal static partial class Lang;
