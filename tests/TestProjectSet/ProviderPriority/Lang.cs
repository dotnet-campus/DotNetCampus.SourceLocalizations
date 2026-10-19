using DotNetCampus.Localizations;

namespace ProviderPriority;

[LocalizedConfiguration(
    Default = "zh-Hans",
    Current = "zh-Hans",
    EnsureKeysIdentical = true,
    GenerationMode = GenerationMode.Dictionary,
    NotificationMode = NotificationMode.InitOnly,
    SupportsAddingProviders = true)]
internal static partial class Lang;
