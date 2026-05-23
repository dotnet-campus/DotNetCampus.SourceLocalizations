// 生成文件：DictNotifiable.Strings.en.g.cs
// 场景：Dictionary + CurrentCulturePropertyChanged + Library
// 说明：Dictionary 模式下每种语言生成一个 Provider 类，内部是 Dictionary<string,string>。
//       与 NotificationMode 无关（Provider 层无感知通知）。

namespace DotNetCampus.Localizations;

internal class LocalizedStringProvider_En(ILocalizedStringProvider? fallback) : ILocalizedStringProvider
{
    public string IetfLanguageTag => "en";

    public string this[string key]
    {
        get
        {
            if (_strings.TryGetValue(key, out var value) && value != null)
            {
                return value;
            }
            if (fallback != null)
            {
                return fallback[key];
            }
            return "";
        }
    }

    private readonly global::System.Collections.Generic.Dictionary<string, string> _strings = new()
    {
        { "A.A1", "Words" },
        { "A.A2", "Error code: {0}" },
        { "A.B.B1", "Hello" },
    };
}
