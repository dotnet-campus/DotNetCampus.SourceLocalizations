#nullable enable
using ILocalizedStringProvider = global::dotnetCampus.Localizations.ILocalizedStringProvider;
using INotifyPropertyChanged = global::System.ComponentModel.INotifyPropertyChanged;
using LocalizedString = global::dotnetCampus.Localizations.LocalizedString;
using PropertyChangedEventHandler = global::System.ComponentModel.PropertyChangedEventHandler;
using StringComparison = global::System.StringComparison;
using System;

namespace dotnetCampus.Localizations.Assets.Templates;

[global::System.Diagnostics.DebuggerDisplay("[{LocalizedStringProvider.IetfLanguageTag}] LOCALIZATION_TYPE_NAME.???")]
[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
internal sealed class ImmutableLocalizedValues(ILocalizedStringProvider provider) : ILocalizedValues
{
    /// <summary>
    /// 获取本地化字符串提供器。
    /// </summary>
    public ILocalizedStringProvider LocalizedStringProvider => provider;

    public string IetfLanguageTag => LocalizedStringProvider.IetfLanguageTag;

    public string this[string key] => LocalizedStringProvider[key];

    // <FLAG>
    // 在此处生成数状结构当前节点的本地化值。
    // public LocalizedString A1 => provider.Get0("A.A1");
    // public LocalizedString<int> A2 => provider.Get1<int>("A.A2");
    // public ILocalizedValues_A_A3 A3 { get; } = new LocalizedValues_A_A3(provider);
    // </FLAG>

    /// <summary>
    /// 获取非完整本地化字符串键的字符串表示。
    /// </summary>
    public override string ToString() => "LOCALIZATION_TYPE_NAME.";
}

// <FLAG2>
// 在此处递归生成树状结构的本地化值。
// </FLAG2>
