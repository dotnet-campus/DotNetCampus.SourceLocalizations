using Microsoft.CodeAnalysis;

namespace dotnetCampus.Localizations.Generators.ModelProviding;

/// <summary>
/// 开发者在代码中指定应基于某个类型生成本地化文件时，此模型表示了开发者所指定的所有需要的生成参数。
/// </summary>
/// <param name="Namespace">类型的命名空间。</param>
/// <param name="TypeName">类型的名称。</param>
/// <param name="DefaultLanguage">默认语言的 IETF 语言标签。</param>
/// <param name="CurrentLanguage">当前语言的 IETF 语言标签。</param>
/// <param name="SupportsNotification">是否支持在修改当前语言时，通知所有的语言项的变更。</param>
/// <param name="EnsureKeysIdentical">指定是否确保所有语言文件中的键都一致。</param>
public readonly record struct LocalizationGeneratingModel(
    string Namespace, string TypeName,
    string DefaultLanguage, string? CurrentLanguage,
    bool SupportsNotification,
    bool EnsureKeysIdentical);
