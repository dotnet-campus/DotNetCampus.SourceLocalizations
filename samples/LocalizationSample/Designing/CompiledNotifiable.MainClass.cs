// 生成文件：CompiledNotifiable.g.cs（主类 partial）
// 场景：Compiled + CurrentCulturePropertyChanged + Library
// 说明：与 Compiled+InitOnly 的区别：
//       1. 主类实现 INotifyPropertyChanged
//       2. _current 是 NotifiableLocalizedValues（包装类），不是直接的编译实现
//       3. SetCurrent 调用 SetInner 递归更新，而非替换实例
//       4. SetCurrent 后 raise PropertyChanged("Current")

using INotifyPropertyChanged = global::System.ComponentModel.INotifyPropertyChanged;
using PropertyChangedEventArgs = global::System.ComponentModel.PropertyChangedEventArgs;
using PropertyChangedEventHandler = global::System.ComponentModel.PropertyChangedEventHandler;

namespace LocalizationSample.Designing;

partial class CompiledNotifiable : INotifyPropertyChanged
{
    private static readonly ILocalizedValues _default = LocalizedValues_En.Instance;
    private static readonly NotifiableLocalizedValues _current;

    static CompiledNotifiable()
    {
        var initialLang = Create(global::System.Globalization.CultureInfo.CurrentUICulture.Name);
        _current = new NotifiableLocalizedValues(initialLang);
    }

    public static global::System.Collections.Generic.IReadOnlyList<string> SupportedLanguageTags { get; } =
    [
        "en",
        "zh-Hans",
    ];

    public static ILocalizedValues Default => _default;

    /// <summary>
    /// 获取当前语言的本地化值。此实例保持不变（引用稳定），切换语言时内部数据会更新并发出通知。
    /// </summary>
    public static NotifiableLocalizedValues Current => _current;

    /// <summary>
    /// 切换当前语言。会递归更新所有子节点的内部引用并 raise PropertyChanged。
    /// </summary>
    public static void SetCurrent(string languageTag)
    {
        var newInner = Create(languageTag);
        _current.SetInner(newInner);
    }

    /// <summary>
    /// 创建一个不可变的编译实现实例（用于内部委托或外部只读使用）。
    /// </summary>
    public static ILocalizedValues Create(string languageTag)
    {
        return languageTag.ToLowerInvariant() switch
        {
            "en" => LocalizedValues_En.Instance,
            "zh-hans" => LocalizedValues_ZhHans.Instance,
            _ => _default,
        };
    }

    // 主类本身的 INPC（用于绑定 CompiledNotifiable.Current 时通知，虽然 Current 引用不变，此处可选）
    public static event PropertyChangedEventHandler? PropertyChanged;
}
