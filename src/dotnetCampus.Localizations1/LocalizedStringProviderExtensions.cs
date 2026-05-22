namespace dotnetCampus.Localizations;

/// <summary>
/// 为 <see cref="ILocalizedStringProvider"/> 提供带有本地化类型返回值的扩展方法。
/// </summary>
public static class LocalizedStringProviderExtensions
{
    /// <summary>
    /// 获取指定键的本地化字符串。
    /// </summary>
    /// <param name="provider">本地化字符串提供者。</param>
    /// <param name="key">要获取的本地化字符串的键。</param>
    /// <returns>一个无参的本地化字符串。</returns>
    public static LocalizedString Get0(this ILocalizedStringProvider provider, string key) =>
        new LocalizedString(key, provider[key]);

    /// <summary>
    /// 获取指定键的有一个参数的本地化字符串。
    /// </summary>
    /// <typeparam name="T1">参数类型。</typeparam>
    /// <param name="provider">本地化字符串提供者。</param>
    /// <param name="key">要获取的本地化字符串的键。</param>
    /// <returns>有一个参数的本地化字符串。</returns>
    public static LocalizedString<T1> Get1<T1>(this ILocalizedStringProvider provider, string key) =>
        new LocalizedString<T1>(key, provider[key]);

    /// <summary>
    /// 获取指定键的有两个参数的本地化字符串。
    /// </summary>
    /// <typeparam name="T1">第一个参数类型。</typeparam>
    /// <typeparam name="T2">第二个参数类型。</typeparam>
    /// <param name="provider">本地化字符串提供者。</param>
    /// <param name="key">要获取的本地化字符串的键。</param>
    /// <returns>有两个参数的本地化字符串。</returns>
    public static LocalizedString<T1, T2> Get2<T1, T2>(this ILocalizedStringProvider provider, string key) =>
        new LocalizedString<T1, T2>(key, provider[key]);

    /// <summary>
    /// 获取指定键的有三个参数的本地化字符串。
    /// </summary>
    /// <typeparam name="T1">第一个参数类型。</typeparam>
    /// <typeparam name="T2">第二个参数类型。</typeparam>
    /// <typeparam name="T3">第三个参数类型。</typeparam>
    /// <param name="provider">本地化字符串提供者。</param>
    /// <param name="key">要获取的本地化字符串的键。</param>
    /// <returns>有三个参数的本地化字符串。</returns>
    public static LocalizedString<T1, T2, T3> Get3<T1, T2, T3>(this ILocalizedStringProvider provider, string key) =>
        new LocalizedString<T1, T2, T3>(key, provider[key]);

    /// <summary>
    /// 获取指定键的有四个参数的本地化字符串。
    /// </summary>
    /// <typeparam name="T1">第一个参数类型。</typeparam>
    /// <typeparam name="T2">第二个参数类型。</typeparam>
    /// <typeparam name="T3">第三个参数类型。</typeparam>
    /// <typeparam name="T4">第四个参数类型。</typeparam>
    /// <param name="provider">本地化字符串提供者。</param>
    /// <param name="key">要获取的本地化字符串的键。</param>
    /// <returns>有四个参数的本地化字符串。</returns>
    public static LocalizedString<T1, T2, T3, T4> Get4<T1, T2, T3, T4>(this ILocalizedStringProvider provider, string key) =>
        new LocalizedString<T1, T2, T3, T4>(key, provider[key]);
}
