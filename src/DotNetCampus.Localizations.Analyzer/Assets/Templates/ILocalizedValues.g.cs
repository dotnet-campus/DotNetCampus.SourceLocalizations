#nullable enable
using global::dotnetCampus.Localizations;
using ILocalizedStringProvider = global::dotnetCampus.Localizations.ILocalizedStringProvider;
using LocalizedString = global::dotnetCampus.Localizations.LocalizedString;

namespace dotnetCampus.Localizations.Assets.Templates;

[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
public partial interface ILocalizedValues : ILocalizedStringProvider
{
    // <FLAG>
    // LocalizedString A1 { get; }
    // LocalizedString<int> A2 { get; }
    // ILocalizedValues_A_A3 A3 { get; }
    // </FLAG>
}

// <FLAG2>
// 在此处递归生成树状结构的本地化值。
// </FLAG2>
