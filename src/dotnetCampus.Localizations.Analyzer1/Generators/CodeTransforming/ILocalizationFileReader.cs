using System.Collections.Immutable;

namespace dotnetCampus.Localizations.Generators.CodeTransforming;

/// <summary>
/// 将不同格式的本地化文件转换为统一的本地化项。
/// </summary>
public interface ILocalizationFileReader
{
    /// <summary>
    /// 读取本地化文件。
    /// </summary>
    /// <param name="content">文件内容。</param>
    /// <returns>此文件的所有本地化项。</returns>
    ImmutableArray<LocalizationItem> Read(string content);
}
