using System;

namespace dotnetCampus.Localizations;

/// <summary>
/// 在一个分部静态类上标记，可以为此静态类生成本地化语言项。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class LocalizedConfigurationAttribute : Attribute
{
    /// <summary>
    /// 指定默认语言。当任何一个语言项未找到时，将使用此语言项。
    /// </summary>
    public required string Default { get; init; }

#if !IN_ANALYZER
    /// <summary>
    /// 指定开发时所用的当前语言。可以在语言项的文档注释中查看此语言项的文本。
    /// </summary>
    /// <remarks>
    /// 如果没有指定，则会使用 <see cref="System.Globalization.CultureInfo.CurrentUICulture"/>。
    /// </remarks>
#endif
    public string? Current { get; init; }

    /// <summary>
    /// 是否支持在修改当前语言时，通知所有的语言项的变更。
    /// </summary>
    public bool SupportsNotification { get; init; }

    /// <summary>
    /// 指定是否确保所有语言文件中的键都是一致的。默认值为 false，不执行检查。如果是 true，则会在生成代码时检查所有语言文件中的键是否一致，如果不相同则会报错。
    /// </summary>
    public bool EnsureKeysIdentical { get; init; }
}
