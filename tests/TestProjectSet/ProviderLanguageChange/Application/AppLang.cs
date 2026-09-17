using DotNetCampus.Localizations;

namespace ProviderLanguageChange.Application;

[LocalizedConfiguration(
    Default = "zh-Hans",
    Current = "zh-Hans",
    EnsureKeysIdentical = true,
    GenerationMode = GenerationMode.Dictionary,
    NotificationMode = NotificationMode.CurrentCulturePropertyChanged)]
internal static partial class AppLang;
