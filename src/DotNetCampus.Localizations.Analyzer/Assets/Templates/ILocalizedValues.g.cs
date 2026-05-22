#nullable enable
using global::DotNetCampus.Localizations;
using ILocalizedStringProvider = global::DotNetCampus.Localizations.ILocalizedStringProvider;
using LocalizedString = global::DotNetCampus.Localizations.LocalizedString;

namespace DotNetCampus.Localizations.Assets.Templates;

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
