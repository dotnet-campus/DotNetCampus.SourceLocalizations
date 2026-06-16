using Microsoft.CodeAnalysis;

namespace DotNetCampus.Localizations.Generators.ModelProviding;

/// <summary>
/// 开发者在代码中指定应基于某个类型生成本地化文件时，此模型表示了开发者所指定的所有需要的生成参数。
/// </summary>
public readonly record struct LocalizationGeneratingModel(string Namespace, string TypeName)
{
    /// <summary>
    /// 用户标记了 <see cref="LocalizedConfigurationAttribute"/> 的类型声明的位置。
    /// </summary>
    public Location? Location { get; init; }

    /// <summary>
    /// 用户声明的类型的可访问性修饰符（"public" 或 "internal"）。
    /// </summary>
    public required string TypeAccessibility { get; init; }

    /// <summary>
    /// 默认语言的 IETF 语言标签。
    /// </summary>
    public required string DefaultLanguage { get; init; }

    /// <summary>
    /// 当前语言的 IETF 语言标签。
    /// </summary>
    public required string? CurrentLanguage { get; init; }

    /// <summary>
    /// 生成模式：Dictionary 或 Compiled。
    /// </summary>
    public required GenerationMode GenerationMode { get; init; }

    /// <summary>
    /// 通知模式：InitOnly、CurrentCulturePropertyChanged 或 LocalizationItemPropertyChanged。
    /// </summary>
    public required NotificationMode NotificationMode { get; init; }

    /// <summary>
    /// 依赖模式：Library 或 NestedSource。
    /// </summary>
    public required DependencyMode DependencyMode { get; init; }

    /// <summary>
    /// 指定是否确保所有语言文件中的键都一致。
    /// </summary>
    public required bool EnsureKeysIdentical { get; init; }
}
