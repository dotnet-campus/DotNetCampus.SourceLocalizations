// 生成文件：CompiledLang.Values.g.cs
// 场景：Compiled + InitOnly + NestedSource
// 说明：Compiled 模式下，每种语言只有一个类，通过显式接口实现所有层级接口。
//       导航属性返回 this，零分配。整个语言只有一个单例对象。

namespace LocalizationSample.Designing;

partial class CompiledLang
{
    // ============================================================
    // en 语言实现（单个类实现所有接口）
    // ============================================================

    internal sealed class LocalizedValues_En : ILocalizedValues, ILocalizedValues_A, ILocalizedValues_A_B
    {
        public static LocalizedValues_En Instance { get; } = new();

        // ILocalizedValues
        ILocalizedValues_A ILocalizedValues.A => this;

        // ILocalizedValues_A
        ILocalizedValues_A_B ILocalizedValues_A.B => this;
        LocalizedString<int> ILocalizedValues_A.A2 => new("A.A2", "Error code: {0}");
        LocalizedString ILocalizedValues_A.A1 => new("A.A1", "Words");

        // ILocalizedValues_A_B
        LocalizedString ILocalizedValues_A_B.B1 => new("A.B.B1", "Hello");
    }

    // ============================================================
    // zh-Hans 语言实现
    // ============================================================

    internal sealed class LocalizedValues_ZhHans : ILocalizedValues, ILocalizedValues_A, ILocalizedValues_A_B
    {
        public static LocalizedValues_ZhHans Instance { get; } = new();

        // ILocalizedValues
        ILocalizedValues_A ILocalizedValues.A => this;

        // ILocalizedValues_A
        ILocalizedValues_A_B ILocalizedValues_A.B => this;
        LocalizedString<int> ILocalizedValues_A.A2 => new("A.A2", "错误码：{0}");
        LocalizedString ILocalizedValues_A.A1 => new("A.A1", "文本");

        // ILocalizedValues_A_B
        LocalizedString ILocalizedValues_A_B.B1 => new("A.B.B1", "你好");
    }
}
