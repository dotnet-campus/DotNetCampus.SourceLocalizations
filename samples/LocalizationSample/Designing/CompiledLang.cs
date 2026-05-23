using DotNetCampus.Localizations;

namespace LocalizationSample.Designing;

/// <summary>
/// 场景：Compiled + InitOnly + NestedSource
/// </summary>
[LocalizedConfiguration(Default = "en", GenerationMode = GenerationMode.Compiled, DependencyMode = DependencyMode.NestedSource)]
internal partial class CompiledLang;
