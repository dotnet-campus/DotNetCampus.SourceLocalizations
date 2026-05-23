using DotNetCampus.Localizations;

namespace LocalizationSample.Designing;

/// <summary>
/// 场景：Dictionary + CurrentCulturePropertyChanged + Library
/// 这就是当前 Lang 的实际行为。
/// </summary>
[LocalizedConfiguration(Default = "en", NotificationMode = NotificationMode.CurrentCulturePropertyChanged)]
internal partial class DictNotifiable;
