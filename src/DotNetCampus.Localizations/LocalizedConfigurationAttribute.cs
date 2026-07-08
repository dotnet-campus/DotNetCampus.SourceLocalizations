using System;
using System.Diagnostics;

namespace DotNetCampus.Localizations;

/// <summary>
/// 在一个分部类上标记，可以为此类生成本地化语言项。
/// </summary>
/// <remarks>
/// 以下修饰符会影响到所生成的类的行为：
/// <list type="bullet">
/// <item>static: 由开发者指定，如果指定静态类，则源生成的 Default/Current 等属性和方法也会是静态的，方便数据绑定；如果不指定，则会是实例的，方便依赖注入。</item>
/// <item>partial: 必须指定，源生成的类会作为此类的另一个部分生成。</item>
/// </list>
/// 其他访问修饰符可随意指定，源生成的类不会用到它们。
/// </remarks>
[Conditional("FOR_SOURCE_GENERATION_ONLY")]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class LocalizedConfigurationAttribute : Attribute
{
    /// <summary>
    /// 指定默认语言。当任何一个语言项未找到时，将使用此语言项。
    /// </summary>
    /// <remarks>
    /// 必须指定默认语言，生成的语言项仅参考此语言所对应的语言文件，不多不少。
    /// </remarks>
    public required string Default { get; init; }

    /// <summary>
    /// 指定运行时的当前语言。
    /// </summary>
    /// <remarks>
    /// 如果没有指定，则会在运行时使用目标计算机的当前语言文化。
    /// </remarks>
    public string? Current { get; init; }

    /// <summary>
    /// 指定是否确保所有语言文件中的键都是一致的。默认值为 false，不执行检查。如果是 true，则会在生成代码时检查所有语言文件中的键是否一致，如果不相同则会报错。
    /// </summary>
    public bool EnsureKeysIdentical { get; init; }

    /// <summary>
    /// 指定生成的本地化类的模式。
    /// </summary>
    public GenerationMode GenerationMode { get; init; }

    /// <summary>
    /// 指定生成本地化相关类时，应如何处理语言文化的变更通知。
    /// </summary>
    public NotificationMode NotificationMode { get; init; }

    /// <summary>
    /// 指定生成本地化相关类时，应如何包含这些类型的依赖。
    /// </summary>
    public DependencyMode DependencyMode { get; init; }

    /// <summary>
    /// 是否支持在修改当前语言时，发出属性变更通知，可用于数据绑定。
    /// </summary>
    [Obsolete("请使用 NotificationMode 属性来指定通知模式。当都设置时，以 NotificationMode 为准。")]
    public bool SupportsNotification
    {
        get => NotificationMode is NotificationMode.CurrentCulturePropertyChanged;
        init => NotificationMode = value switch
        {
            true => NotificationMode.CurrentCulturePropertyChanged,
            false => NotificationMode.InitOnly,
        };
    }
}

/// <summary>
/// 指定生成的本地化类的模式。
/// </summary>
public enum GenerationMode
{
    /// <summary>
    /// 所有语言项都在运行时从字典中获取，这将允许动态生成语音项的 Key，获取仅运行时才能确定的语言项。
    /// </summary>
    /// <remarks>
    /// 会为当前语言生成一个字典，包含所有语言项。
    /// </remarks>
    Dictionary,

    /// <summary>
    /// 所有语言项都被编译，只能使用编译时确定的方式访问语言项。
    /// </summary>
    /// <remarks>
    /// 这能最大化减少运行时的开销。
    /// </remarks>
    Compiled,
}

/// <summary>
/// 指定生成的本地化类是否支持在语言文化变更时，发出属性变更通知。
/// </summary>
public enum NotificationMode
{
    /// <summary>
    /// 自初始化完成后，当前语言文化和任何语言项不再变化，因此也不会发起变更通知。
    /// </summary>
    InitOnly,

    /// <summary>
    /// 各语言项不可变，但改变当前语言文化时，会发出属性变更通知。
    /// </summary>
    CurrentCulturePropertyChanged,

    /// <summary>
    /// 改变当前语言文化或任何语言项时，都会发出属性变更通知。基本没有什么业务场景会有改变单个语言项的需求，因此此类别也没什么用。
    /// </summary>
    LocalizationItemPropertyChanged,
}

/// <summary>
/// 指定生成本地化相关类时，应如何包含这些类型的依赖。
/// </summary>
public enum DependencyMode
{
    /// <summary>
    /// 本地化所需的基础类型会使用库中的，因此运行时需要保证存在 dotNetCampus.SourceLocalizations.dll（默认就会包含）。<br/>
    /// 更适合比较完整的大型或小型应用程序。
    /// </summary>
    Library,

    /// <summary>
    /// 将所有依赖的类型生成到内部类中，无任何运行时依赖，整个 DotNetCampus.SourceLocalizations.dll 甚至都可以直接删除。<br/>
    /// 适合单文件应用程序，或仅希望获得多语言支持而不希望引入额外依赖的库项目。
    /// </summary>
    NestedSource,
}
