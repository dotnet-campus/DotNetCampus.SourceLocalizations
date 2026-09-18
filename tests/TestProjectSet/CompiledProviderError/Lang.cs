using DotNetCampus.Localizations;

namespace CompiledProviderError;

[LocalizedConfiguration(
    Default = "zh-Hans",
    EnsureKeysIdentical = true,
    GenerationMode = GenerationMode.Compiled,
    SupportsAddingProviders = true)]
internal static partial class Lang;
