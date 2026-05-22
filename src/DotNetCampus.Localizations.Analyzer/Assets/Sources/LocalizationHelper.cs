using System.Linq;
// ReSharper disable once CheckNamespace
using System.Collections.Generic;

namespace DotNetCampus.Localizations.Helpers;

public static class LocalizationHelper
{
    public static string? MatchWithFallback(string requestedIetfLanguageTag, IEnumerable<string> availableIetfLanguageTags)
    {
        return availableIetfLanguageTags.FirstOrDefault();
    }
}
