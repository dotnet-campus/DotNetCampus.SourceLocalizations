namespace dotnetCampus.Localizations.Generators.ModelProviding;

/// <summary>
/// 本地化语言文件模型。
/// </summary>
/// <param name="FileFormat">本地化语言文件的类型。</param>
/// <param name="IetfLanguageTag">此本地化语言文件所对应的 IETF 语言标签。</param>
/// <param name="Content">本地化语言文件的内容。</param>
public readonly record struct LocalizationFileModel(string FileFormat, string IetfLanguageTag, string Content);
