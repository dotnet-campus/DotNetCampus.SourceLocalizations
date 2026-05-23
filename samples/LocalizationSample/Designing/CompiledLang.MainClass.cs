// 生成文件：CompiledLang.g.cs（主类 partial）
// 场景：Compiled + InitOnly + NestedSource
// 说明：主类提供 Default/Current/SetCurrent/Create 等入口。
//       Compiled+InitOnly 下 SetCurrent 为 void，直接替换实例。
//       不存在 Provider，工厂方法直接返回对应语言的编译实现。

namespace LocalizationSample.Designing;

partial class CompiledLang
{
    private static readonly ILocalizedValues _default = LocalizedValues_En.Instance;
    private static ILocalizedValues _current;

    static CompiledLang()
    {
        _current = Create(global::System.Globalization.CultureInfo.CurrentUICulture.Name);
    }

    public static global::System.Collections.Generic.IReadOnlyList<string> SupportedLanguageTags { get; } =
    [
        "en",
        "zh-Hans",
    ];

    public static ILocalizedValues Default => _default;

    public static ILocalizedValues Current => _current;

    public static void SetCurrent(string languageTag)
    {
        _current = Create(languageTag);
    }

    public static ILocalizedValues Create(string languageTag)
    {
        return languageTag.ToLowerInvariant() switch
        {
            "en" => LocalizedValues_En.Instance,
            "zh-hans" => LocalizedValues_ZhHans.Instance,
            _ => (ILocalizedValues)_default,
        };
    }
}
