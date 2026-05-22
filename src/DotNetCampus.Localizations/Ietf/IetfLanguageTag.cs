using System;
using System.Globalization;

namespace dotnetCampus.Localizations.Ietf;

/// <summary>
/// 表示符合 IETF BCP 47 标准的语言标签。
/// </summary>
/// <remarks>
/// 关于 IETF BCP 47 标准的语言标签，请参见：https://en.wikipedia.org/wiki/IETF_language_tag
/// <para/>
/// 数据由 INNA 提供，参见：https://www.iana.org/assignments/language-subtag-registry/language-subtag-registry
/// </remarks>
public readonly record struct IetfLanguageTag
{
    private readonly CultureInfo _cultureInfo;

    /// <summary>
    /// 初始化 <see cref="IetfLanguageTag"/> 类的新实例。
    /// </summary>
    /// <param name="ietfLanguageTag">符合 IETF BCP 47 标准的语言标签。</param>
    public IetfLanguageTag(string ietfLanguageTag)
    {
        _cultureInfo = CultureInfo.GetCultureInfo(ietfLanguageTag);
    }

    /// <summary>
    /// 初始化 <see cref="IetfLanguageTag"/> 类的新实例。
    /// </summary>
    /// <param name="cultureInfo">区域性信息。</param>
    public IetfLanguageTag(CultureInfo cultureInfo)
    {
        _cultureInfo = cultureInfo.IsReadOnly
            ? CultureInfo.GetCultureInfo(cultureInfo.Name)
            : cultureInfo;
    }

    /// <summary>
    /// 获取当前 IETF 语言标签的字符串表示形式。
    /// </summary>
    /// <remarks>
    /// 虽然微软官方文档说明 <see cref="CultureInfo.IetfLanguageTag"/> 已被弃用，应改成 <see cref="CultureInfo.Name"/>，
    /// 但实际上有且只有两个区域这两个值不同：zh-CHS 和 zh-CHT。<br/>
    /// zh-CHS 的 Name 为 zh-CHS（被弃用）而 IetfLanguageTag 为 zh-Hans（符合标准）；
    /// zh-CHT 的 Name 为 zh-CHT（被弃用）而 IetfLanguageTag 为 zh-Hant（符合标准）。
    /// </remarks>
    public string Value => _cultureInfo.IetfLanguageTag;

    /// <summary>
    /// 获取当前 IETF 语言标签的字符串表示形式。
    /// </summary>
    /// <returns>当前 IETF 语言标签的字符串表示形式。</returns>
    public override string ToString() => Value;

    /// <summary>
    /// 获取当前 IETF 语言标签的字符串表示形式。
    /// </summary>
    /// <param name="ietfLanguageTag">IETF 语言标签。</param>
    /// <returns></returns>
    public static implicit operator string(IetfLanguageTag ietfLanguageTag) => ietfLanguageTag.Value;

    /// <summary>
    /// 从字符串创建 <see cref="IetfLanguageTag"/> 实例。
    /// </summary>
    /// <param name="ietfLanguageTag">
    /// 符合 IETF BCP 47 标准的语言标签字符串。
    /// 如果字符串不符合 IETF BCP 47 标准，将会抛出 <see cref="ArgumentException"/> 异常。
    /// <para>特别的，对于不符合 IETF BCP 47 标准但在 Windows 系统中可用的区域名称（如 zh-CN、zh-HK、zh-TW 等），此方法会自动将其转换为 IETF 语言标签。</para>
    /// </param>
    /// <returns>IETF 语言标签。</returns>
    /// <exception cref="ArgumentException">字符串不符合 IETF BCP 47 标准。</exception>
    public static implicit operator IetfLanguageTag(string ietfLanguageTag) => new(ietfLanguageTag);
}
