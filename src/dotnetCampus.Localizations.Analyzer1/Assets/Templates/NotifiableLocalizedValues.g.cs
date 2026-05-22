#nullable enable
using ILocalizedStringProvider = global::dotnetCampus.Localizations.ILocalizedStringProvider;
using INotifyPropertyChanged = global::System.ComponentModel.INotifyPropertyChanged;
using LocalizedString = global::dotnetCampus.Localizations.LocalizedString;
using PropertyChangedEventArgs = global::System.ComponentModel.PropertyChangedEventArgs;
using PropertyChangedEventHandler = global::System.ComponentModel.PropertyChangedEventHandler;
using System;
using ArgumentNullException = global::System.ArgumentNullException;

namespace dotnetCampus.Localizations.Assets.Templates;

[global::System.Diagnostics.DebuggerDisplay("[{LocalizedStringProvider.IetfLanguageTag}] LOCALIZATION_TYPE_NAME.???")]
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
internal sealed class NotifiableLocalizedValues(ILocalizedStringProvider provider) : ILocalizedValues, INotifyPropertyChanged
{
    /// <summary>
    /// 获取本地化字符串提供器。
    /// </summary>
    public ILocalizedStringProvider LocalizedStringProvider { get; private set; } = provider;

    public string IetfLanguageTag => LocalizedStringProvider.IetfLanguageTag;

    public string this[string key] => LocalizedStringProvider[key];

    // <FLAG3>
    /// <summary>
    /// 在不改变 <see cref="LocalizedStringProvider"/> 实例的情况下，设置新的本地化字符串提供器，并通知所有的属性的变更。
    /// </summary>
    /// <param name="newProvider">新的本地化字符串提供器。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="newProvider"/> 为 null 时抛出。</exception>
    internal async global::System.Threading.Tasks.ValueTask SetProvider(ILocalizedStringProvider newProvider)
    {
        if (newProvider is null)
        {
            throw new ArgumentNullException(nameof(newProvider));
        }

        var oldProvider = LocalizedStringProvider;
        if (oldProvider == newProvider)
        {
            return;
        }
        LocalizedStringProvider = newProvider;

        // 在此处为所有的属性添加 PropertyChanged 事件。
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
        await global::System.Threading.Tasks.Task.Yield();
    }
    // </FLAG3>

    // <FLAG>
    // 在此处生成数状结构当前节点的本地化值。
    // public LocalizedString A1 => provider.Get0("A.A1");
    // public LocalizedString<int> A2 => provider.Get1<int>("A.A2");
    // public ILocalizedValues_A_A3 A3 { get; } = new LocalizedValues_A_A3(provider);
    // </FLAG>

#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    /// <summary>
    /// 获取非完整本地化字符串键的字符串表示。
    /// </summary>
    public override string ToString() => "LOCALIZATION_TYPE_NAME.";
}

// <FLAG2>
// 在此处递归生成树状结构的本地化值。
// </FLAG2>
