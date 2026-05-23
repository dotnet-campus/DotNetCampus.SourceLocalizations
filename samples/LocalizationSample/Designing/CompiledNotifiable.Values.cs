// 生成文件：CompiledNotifiable.Values.g.cs
// 场景：Compiled + CurrentCulturePropertyChanged + Library
// 说明：
//   Compiled 不需要照顾 WPF（WPF 不认显式接口实现），所以可以用单类方案。
//   Immutable：每语言一个单例类，显式接口实现，零分配。
//   Notifiable：单类包装，持有 _inner 引用，切换时替换 + raise 所有叶子属性名。
//   非 WPF 框架（Avalonia/WinUI/Uno）可以正确绑定显式接口属性。

using INotifyPropertyChanged = global::System.ComponentModel.INotifyPropertyChanged;
using PropertyChangedEventArgs = global::System.ComponentModel.PropertyChangedEventArgs;
using PropertyChangedEventHandler = global::System.ComponentModel.PropertyChangedEventHandler;
using LocalizedString = global::DotNetCampus.Localizations.LocalizedString;

namespace DotNetCampus.Localizations;

// ============================================================
// Immutable 实现（每语言一个单例类，显式接口实现）
// ============================================================

internal sealed class LocalizedValues_En : ILocalizedValues, ILocalizedValues_A, ILocalizedValues_A_B
{
    public static LocalizedValues_En Instance { get; } = new();

    ILocalizedValues_A ILocalizedValues.A => this;
    ILocalizedValues_A_B ILocalizedValues_A.B => this;
    LocalizedString<int> ILocalizedValues_A.A2 => new("A.A2", "Error code: {0}");
    LocalizedString ILocalizedValues_A.A1 => new("A.A1", "Words");
    LocalizedString ILocalizedValues_A_B.B1 => new("A.B.B1", "Hello");
}

internal sealed class LocalizedValues_ZhHans : ILocalizedValues, ILocalizedValues_A, ILocalizedValues_A_B
{
    public static LocalizedValues_ZhHans Instance { get; } = new();

    ILocalizedValues_A ILocalizedValues.A => this;
    ILocalizedValues_A_B ILocalizedValues_A.B => this;
    LocalizedString<int> ILocalizedValues_A.A2 => new("A.A2", "错误码：{0}");
    LocalizedString ILocalizedValues_A.A1 => new("A.A1", "文本");
    LocalizedString ILocalizedValues_A_B.B1 => new("A.B.B1", "你好");
}

// ============================================================
// Notifiable 包装（单类，显式接口实现 + INPC）
// 所有导航属性返回 this，所有叶子属性委托到 _inner。
// 非 WPF 框架中，绑定路径 A.B.B1 最终取到的是 this 的显式接口成员，
// 切换语言后 raise "B1" 等叶子属性名即可触发 UI 更新。
// ============================================================

internal sealed class NotifiableLocalizedValues : ILocalizedValues, ILocalizedValues_A, ILocalizedValues_A_B, INotifyPropertyChanged
{
    private ILocalizedValues _inner;
    private ILocalizedValues_A _innerA;
    private ILocalizedValues_A_B _innerAB;

    public NotifiableLocalizedValues(ILocalizedValues inner)
    {
        _inner = inner;
        _innerA = inner.A;
        _innerAB = inner.A.B;
    }

    // ILocalizedValues
    ILocalizedValues_A ILocalizedValues.A => this;

    // ILocalizedValues_A
    ILocalizedValues_A_B ILocalizedValues_A.B => this;
    LocalizedString<int> ILocalizedValues_A.A2 => _innerA.A2;
    LocalizedString ILocalizedValues_A.A1 => _innerA.A1;

    // ILocalizedValues_A_B
    LocalizedString ILocalizedValues_A_B.B1 => _innerAB.B1;

    internal void SetInner(ILocalizedValues newInner)
    {
        _inner = newInner;
        _innerA = newInner.A;
        _innerAB = newInner.A.B;

        // raise 所有叶子属性名
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("A1"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("A2"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("B1"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
