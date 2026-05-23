using DotNetCampus.Localizations;

namespace LocalizationSample.Designing;

/// <summary>
/// 场景：Compiled + CurrentCulturePropertyChanged + Library
/// </summary>
[LocalizedConfiguration(Default = "en", GenerationMode = GenerationMode.Compiled, NotificationMode = NotificationMode.CurrentCulturePropertyChanged)]
internal partial class CompiledNotifiable;
