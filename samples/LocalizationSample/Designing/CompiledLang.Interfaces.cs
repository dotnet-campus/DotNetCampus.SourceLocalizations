// 生成文件：CompiledLang.Interfaces.g.cs
// 场景：Compiled + InitOnly + NestedSource
// 说明：NestedSource 模式下，接口树作为 partial class 的内部类型生成。
//       接口树与 GenerationMode/NotificationMode 无关，仅由 key 树结构决定。

namespace LocalizationSample.Designing;

partial class CompiledLang
{
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal interface ILocalizedValues
    {
        ILocalizedValues_A A { get; }
    }

    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal interface ILocalizedValues_A
    {
        ILocalizedValues_A_B B { get; }

        /// <summary>
        /// Error code: {errorCode:int}
        /// </summary>
        LocalizedString<int> A2 { get; }

        /// <summary>
        /// Words
        /// </summary>
        LocalizedString A1 { get; }
    }

    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal interface ILocalizedValues_A_B
    {
        /// <summary>
        /// Hello
        /// </summary>
        LocalizedString B1 { get; }
    }
}
