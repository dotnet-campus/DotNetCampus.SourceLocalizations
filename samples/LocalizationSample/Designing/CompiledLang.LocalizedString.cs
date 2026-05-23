// 生成文件：CompiledLang.LocalizedString.g.cs
// 场景：仅 NestedSource 模式需要生成此文件
// 说明：当 DependencyMode = NestedSource 时，LocalizedString 等基础类型作为内部类型生成，
//       使项目无需引用 DotNetCampus.Localizations.dll。
//       当 DependencyMode = Library 时，此文件不生成，使用 dll 中的同名类型。

namespace LocalizationSample.Designing;

partial class CompiledLang
{
    public readonly record struct LocalizedString
    {
        private readonly string _key;

        public LocalizedString(string key, string value)
        {
            _key = key;
            Value = value;
        }

        public string Value { get; }

        public static implicit operator string(LocalizedString localizedString) => localizedString.Value;

        public override string ToString() => Value;
    }

    public readonly record struct LocalizedString<T1>
    {
        private readonly string _key;
        private readonly string _value;

        public LocalizedString(string key, string value)
        {
            _key = key;
            _value = value;
        }

        public string ToString(T1 arg1) => string.Format(_value, arg1);

        [global::System.Obsolete("请使用带参数的 ToString 方法。", true)]
        public override string ToString() => _value;
    }
}
