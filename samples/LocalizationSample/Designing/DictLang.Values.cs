// 生成文件：DictLang.Values.g.cs
// 场景：Dictionary + InitOnly + Library
// 说明：Dictionary 必须照顾 WPF 绑定（WPF 不认显式接口实现），
//       因此保留多类树方案：每个节点一个类，公开属性实现接口。

using ILocalizedStringProvider = global::DotNetCampus.Localizations.ILocalizedStringProvider;
using LocalizedString = global::DotNetCampus.Localizations.LocalizedString;

namespace DotNetCampus.Localizations;

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
