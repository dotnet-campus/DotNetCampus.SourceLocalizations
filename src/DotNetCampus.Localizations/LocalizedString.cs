using System;
using System.Diagnostics;

#pragma warning disable CS0809 // 过时成员重写未过时成员
namespace DotNetCampus.Localizations;

/// <summary>
/// 表示一个本地化字符串，可隐式转换为字符串。
/// </summary>
[DebuggerDisplay("{_key} = \"{Value}\"")]
public readonly record struct LocalizedString
{
    private readonly string _key;

    /// <summary>
    /// 初始化一个新的本地化字符串实例。
    /// </summary>
    /// <param name="key">多语言键。</param>
    /// <param name="value">多语言值。</param>
    public LocalizedString(string key, string value)
    {
        _key = key;
        Value = value;
    }

    /// <summary>
    /// 获取多语言值。
    /// </summary>
    /// <remarks>
    /// 只有无参本地化字符串具有 <see cref="Value"/> 属性，可用于不可控的弱类型代码中，要求必须传入 string 实例时使用（如 XAML 绑定）。
    /// </remarks>
    public string Value { get; }

    /// <summary>
    /// 隐式转换为字符串。
    /// </summary>
    /// <param name="localizedString">要转换的本地化字符串。</param>
    public static implicit operator string(LocalizedString localizedString) => localizedString.Value;

    /// <summary>
    /// 将本地化字符串转换为字符串。
    /// </summary>
    /// <returns>转换的字符串。</returns>
    public override string ToString() => Value;
}

/// <summary>
/// 表示一个带有一个参数的本地化字符串。
/// </summary>
/// <typeparam name="T1">参数类型。</typeparam>
[DebuggerDisplay("{_key} = \"{_value}\"")]
public readonly record struct LocalizedString<T1>
{
    private readonly string _key;
    private readonly string _value;

    /// <summary>
    /// 初始化一个新的本地化字符串实例。
    /// </summary>
    /// <param name="key">多语言键。</param>
    /// <param name="value">多语言值。</param>
    public LocalizedString(string key, string value)
    {
        _key = key;
        _value = value;
    }

    /// <summary>
    /// 格式化本地化字符串。
    /// </summary>
    /// <param name="arg1">要格式化的参数。</param>
    /// <returns>格式化后的字符串。</returns>
    public string ToString(T1 arg1) => string.Format(_value, arg1);

    /// <summary>
    /// 不要使用！请使用带参数的 ToString 方法。
    /// </summary>
    [Obsolete("请使用带参数的 ToString 方法。", true)]
    public override string ToString() => _value;
}

/// <summary>
/// 表示一个带有两个参数的本地化字符串。
/// </summary>
/// <typeparam name="T1">第一个参数类型。</typeparam>
/// <typeparam name="T2">第二个参数类型。</typeparam>
[DebuggerDisplay("{_key} = \"{_value}\"")]
public readonly record struct LocalizedString<T1, T2>
{
    private readonly string _key;
    private readonly string _value;

    /// <summary>
    /// 初始化一个新的本地化字符串实例。
    /// </summary>
    /// <param name="key">多语言键。</param>
    /// <param name="value">多语言值。</param>
    public LocalizedString(string key, string value)
    {
        _key = key;
        _value = value;
    }

    /// <summary>
    /// 格式化本地化字符串。
    /// </summary>
    /// <param name="arg1">要格式化的参数。</param>
    /// <param name="arg2">要格式化的参数。</param>
    /// <returns>格式化后的字符串。</returns>
    public string ToString(T1 arg1, T2 arg2) => string.Format(_value, arg1, arg2);

    /// <summary>
    /// 不要使用！请使用带参数的 ToString 方法。
    /// </summary>
    [Obsolete("请使用带参数的 ToString 方法。", true)]
    public override string ToString() => _value;
}

/// <summary>
/// 表示一个带有三个参数的本地化字符串。
/// </summary>
/// <typeparam name="T1">第一个参数类型。</typeparam>
/// <typeparam name="T2">第二个参数类型。</typeparam>
/// <typeparam name="T3">第三个参数类型。</typeparam>
[DebuggerDisplay("{_key} = \"{_value}\"")]
public readonly record struct LocalizedString<T1, T2, T3>
{
    private readonly string _key;
    private readonly string _value;

    /// <summary>
    /// 初始化一个新的本地化字符串实例。
    /// </summary>
    /// <param name="key">多语言键。</param>
    /// <param name="value">多语言值。</param>
    public LocalizedString(string key, string value)
    {
        _key = key;
        _value = value;
    }

    /// <summary>
    /// 格式化本地化字符串。
    /// </summary>
    /// <param name="arg1">要格式化的参数。</param>
    /// <param name="arg2">要格式化的参数。</param>
    /// <param name="arg3">要格式化的参数。</param>
    /// <returns>格式化后的字符串。</returns>
    public string ToString(T1 arg1, T2 arg2, T3 arg3) => string.Format(_value, arg1, arg2, arg3);

    /// <summary>
    /// 不要使用！请使用带参数的 ToString 方法。
    /// </summary>
    [Obsolete("请使用带参数的 ToString 方法。", true)]
    public override string ToString() => _value;
}

/// <summary>
/// 表示一个带有四个参数的本地化字符串。
/// </summary>
/// <typeparam name="T1">第一个参数类型。</typeparam>
/// <typeparam name="T2">第二个参数类型。</typeparam>
/// <typeparam name="T3">第三个参数类型。</typeparam>
/// <typeparam name="T4">第四个参数类型。</typeparam>
[DebuggerDisplay("{_key} = \"{_value}\"")]
public readonly record struct LocalizedString<T1, T2, T3, T4>
{
    private readonly string _key;
    private readonly string _value;

    /// <summary>
    /// 初始化一个新的本地化字符串实例。
    /// </summary>
    /// <param name="key">多语言键。</param>
    /// <param name="value">多语言值。</param>
    public LocalizedString(string key, string value)
    {
        _key = key;
        _value = value;
    }

    /// <summary>
    /// 格式化本地化字符串。
    /// </summary>
    /// <param name="arg1">要格式化的参数。</param>
    /// <param name="arg2">要格式化的参数。</param>
    /// <param name="arg3">要格式化的参数。</param>
    /// <param name="arg4">要格式化的参数。</param>
    /// <returns>格式化后的字符串。</returns>
    public string ToString(T1 arg1, T2 arg2, T3 arg3, T4 arg4) => string.Format(_value, arg1, arg2, arg3, arg4);

    /// <summary>
    /// 不要使用！请使用带参数的 ToString 方法。
    /// </summary>
    [Obsolete("请使用带参数的 ToString 方法。", true)]
    public override string ToString() => _value;
}

/// <summary>
/// 表示一个带有五个参数的本地化字符串。
/// </summary>
/// <typeparam name="T1">第一个参数类型。</typeparam>
/// <typeparam name="T2">第二个参数类型。</typeparam>
/// <typeparam name="T3">第三个参数类型。</typeparam>
/// <typeparam name="T4">第四个参数类型。</typeparam>
/// <typeparam name="T5">第五个参数类型。</typeparam>
[DebuggerDisplay("{_key} = \"{_value}\"")]
public readonly record struct LocalizedString<T1, T2, T3, T4, T5>
{
    private readonly string _key;
    private readonly string _value;

    /// <summary>
    /// 初始化一个新的本地化字符串实例。
    /// </summary>
    /// <param name="key">多语言键。</param>
    /// <param name="value">多语言值。</param>
    public LocalizedString(string key, string value)
    {
        _key = key;
        _value = value;
    }

    /// <summary>
    /// 格式化本地化字符串。
    /// </summary>
    public string ToString(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => string.Format(_value, arg1, arg2, arg3, arg4, arg5);

    /// <summary>
    /// 不要使用！请使用带参数的 ToString 方法。
    /// </summary>
    [Obsolete("请使用带参数的 ToString 方法。", true)]
    public override string ToString() => _value;
}

/// <summary>
/// 表示一个带有六个参数的本地化字符串。
/// </summary>
/// <typeparam name="T1">第一个参数类型。</typeparam>
/// <typeparam name="T2">第二个参数类型。</typeparam>
/// <typeparam name="T3">第三个参数类型。</typeparam>
/// <typeparam name="T4">第四个参数类型。</typeparam>
/// <typeparam name="T5">第五个参数类型。</typeparam>
/// <typeparam name="T6">第六个参数类型。</typeparam>
[DebuggerDisplay("{_key} = \"{_value}\"")]
public readonly record struct LocalizedString<T1, T2, T3, T4, T5, T6>
{
    private readonly string _key;
    private readonly string _value;

    /// <summary>
    /// 初始化一个新的本地化字符串实例。
    /// </summary>
    /// <param name="key">多语言键。</param>
    /// <param name="value">多语言值。</param>
    public LocalizedString(string key, string value)
    {
        _key = key;
        _value = value;
    }

    /// <summary>
    /// 格式化本地化字符串。
    /// </summary>
    public string ToString(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => string.Format(_value, arg1, arg2, arg3, arg4, arg5, arg6);

    /// <summary>
    /// 不要使用！请使用带参数的 ToString 方法。
    /// </summary>
    [Obsolete("请使用带参数的 ToString 方法。", true)]
    public override string ToString() => _value;
}

/// <summary>
/// 表示一个带有七个参数的本地化字符串。
/// </summary>
/// <typeparam name="T1">第一个参数类型。</typeparam>
/// <typeparam name="T2">第二个参数类型。</typeparam>
/// <typeparam name="T3">第三个参数类型。</typeparam>
/// <typeparam name="T4">第四个参数类型。</typeparam>
/// <typeparam name="T5">第五个参数类型。</typeparam>
/// <typeparam name="T6">第六个参数类型。</typeparam>
/// <typeparam name="T7">第七个参数类型。</typeparam>
[DebuggerDisplay("{_key} = \"{_value}\"")]
public readonly record struct LocalizedString<T1, T2, T3, T4, T5, T6, T7>
{
    private readonly string _key;
    private readonly string _value;

    /// <summary>
    /// 初始化一个新的本地化字符串实例。
    /// </summary>
    /// <param name="key">多语言键。</param>
    /// <param name="value">多语言值。</param>
    public LocalizedString(string key, string value)
    {
        _key = key;
        _value = value;
    }

    /// <summary>
    /// 格式化本地化字符串。
    /// </summary>
    public string ToString(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) => string.Format(_value, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

    /// <summary>
    /// 不要使用！请使用带参数的 ToString 方法。
    /// </summary>
    [Obsolete("请使用带参数的 ToString 方法。", true)]
    public override string ToString() => _value;
}

/// <summary>
/// 表示一个带有八个参数的本地化字符串。
/// </summary>
/// <typeparam name="T1">第一个参数类型。</typeparam>
/// <typeparam name="T2">第二个参数类型。</typeparam>
/// <typeparam name="T3">第三个参数类型。</typeparam>
/// <typeparam name="T4">第四个参数类型。</typeparam>
/// <typeparam name="T5">第五个参数类型。</typeparam>
/// <typeparam name="T6">第六个参数类型。</typeparam>
/// <typeparam name="T7">第七个参数类型。</typeparam>
/// <typeparam name="T8">第八个参数类型。</typeparam>
[DebuggerDisplay("{_key} = \"{_value}\"")]
public readonly record struct LocalizedString<T1, T2, T3, T4, T5, T6, T7, T8>
{
    private readonly string _key;
    private readonly string _value;

    /// <summary>
    /// 初始化一个新的本地化字符串实例。
    /// </summary>
    /// <param name="key">多语言键。</param>
    /// <param name="value">多语言值。</param>
    public LocalizedString(string key, string value)
    {
        _key = key;
        _value = value;
    }

    /// <summary>
    /// 格式化本地化字符串。
    /// </summary>
    public string ToString(T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) => string.Format(_value, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

    /// <summary>
    /// 不要使用！请使用带参数的 ToString 方法。
    /// </summary>
    [Obsolete("请使用带参数的 ToString 方法。", true)]
    public override string ToString() => _value;
}
