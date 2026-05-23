// 生成文件：DictNotifiable.Values.g.cs
// 场景：Dictionary + CurrentCulturePropertyChanged + Library
// 说明：Dictionary 必须照顾 WPF，所以保留多类树 + 每节点 INPC + 递归 SetProvider。
//       这与当前生成的代码逻辑一致（已验证可工作于 WPF/Avalonia/WinUI）。

using ILocalizedStringProvider = global::DotNetCampus.Localizations.ILocalizedStringProvider;
using INotifyPropertyChanged = global::System.ComponentModel.INotifyPropertyChanged;
using LocalizedString = global::DotNetCampus.Localizations.LocalizedString;
using PropertyChangedEventArgs = global::System.ComponentModel.PropertyChangedEventArgs;
using PropertyChangedEventHandler = global::System.ComponentModel.PropertyChangedEventHandler;

namespace DotNetCampus.Localizations;

// ============================================================
// Immutable 实现（用于 Default，与 DictLang.Values 完全相同）
// ============================================================

internal sealed class ImmutableLocalizedValues(ILocalizedStringProvider provider) : ILocalizedValues
{
    public ILocalizedStringProvider LocalizedStringProvider => provider;
    public string IetfLanguageTag => provider.IetfLanguageTag;
    public string this[string key] => provider[key];
    public ILocalizedValues_A A { get; } = new ImmutableLocalizedValues_A(provider);
}

internal sealed class ImmutableLocalizedValues_A(ILocalizedStringProvider provider) : ILocalizedValues_A
{
    public ILocalizedValues_A_B B { get; } = new ImmutableLocalizedValues_A_B(provider);
    public LocalizedString<int> A2 => provider.Get1<int>("A.A2");
    public LocalizedString A1 => provider.Get0("A.A1");
}

internal sealed class ImmutableLocalizedValues_A_B(ILocalizedStringProvider provider) : ILocalizedValues_A_B
{
    public LocalizedString B1 => provider.Get0("A.B.B1");
}

// ============================================================
// Notifiable 实现（用于 Current，每节点 INPC + 递归 SetProvider）
// WPF 绑定路径 A.B.B1 时，会逐级监听每个对象的 PropertyChanged，
// 所以每个节点必须是独立对象 + 各自 raise 自己的叶子属性。
// ============================================================

internal sealed class NotifiableLocalizedValues : ILocalizedValues, INotifyPropertyChanged
{
    public ILocalizedStringProvider LocalizedStringProvider { get; private set; }

    public NotifiableLocalizedValues(ILocalizedStringProvider provider)
    {
        LocalizedStringProvider = provider;
        A = new NotifiableLocalizedValues_A(provider);
    }

    public string IetfLanguageTag => LocalizedStringProvider.IetfLanguageTag;
    public string this[string key] => LocalizedStringProvider[key];
    public ILocalizedValues_A A { get; }

    internal void SetProvider(ILocalizedStringProvider newProvider)
    {
        LocalizedStringProvider = newProvider;
        ((NotifiableLocalizedValues_A)A).SetProvider(newProvider);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

internal sealed class NotifiableLocalizedValues_A : ILocalizedValues_A, INotifyPropertyChanged
{
    public ILocalizedStringProvider LocalizedStringProvider { get; private set; }

    public NotifiableLocalizedValues_A(ILocalizedStringProvider provider)
    {
        LocalizedStringProvider = provider;
        B = new NotifiableLocalizedValues_A_B(provider);
    }

    public ILocalizedValues_A_B B { get; }
    public LocalizedString<int> A2 => LocalizedStringProvider.Get1<int>("A.A2");
    public LocalizedString A1 => LocalizedStringProvider.Get0("A.A1");

    internal void SetProvider(ILocalizedStringProvider newProvider)
    {
        LocalizedStringProvider = newProvider;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("A2"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("A1"));
        ((NotifiableLocalizedValues_A_B)B).SetProvider(newProvider);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

internal sealed class NotifiableLocalizedValues_A_B : ILocalizedValues_A_B, INotifyPropertyChanged
{
    public ILocalizedStringProvider LocalizedStringProvider { get; private set; }

    public NotifiableLocalizedValues_A_B(ILocalizedStringProvider provider)
    {
        LocalizedStringProvider = provider;
    }

    public LocalizedString B1 => LocalizedStringProvider.Get0("A.B.B1");

    internal void SetProvider(ILocalizedStringProvider newProvider)
    {
        LocalizedStringProvider = newProvider;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("B1"));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
