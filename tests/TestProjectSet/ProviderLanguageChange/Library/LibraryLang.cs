using DotNetCampus.Localizations;

namespace ProviderLanguageChange.Library;

[LocalizedConfiguration(
    Default = "zh-Hans",
    Current = "zh-Hans",
    AreIdenticalKeysRequired = true,
    GenerationMode = GenerationMode.Dictionary,
    NotificationMode = NotificationMode.CurrentCulturePropertyChanged,
    SupportsAddingProviders = true)]
public static partial class LibraryLang;
