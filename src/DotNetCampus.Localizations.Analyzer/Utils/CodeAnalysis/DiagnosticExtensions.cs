using Microsoft.CodeAnalysis;

namespace DotNetCampus.Localizations.Utils.CodeAnalysis;

public static class DiagnosticExtensions
{
    public static void ReportUnknownError(this SourceProductionContext context, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DLA000_UnknownError,
            null,
            message));
    }

    public static void ReportDefaultLanguageTagNotFound(this SourceProductionContext context, Location? location, string defaultTag, string availableTags)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DLA001_DefaultLanguageTagIsNotInTheTagList,
            location,
            defaultTag,
            availableTags));
    }

    public static void ReportCurrentLanguageTagNotFound(this SourceProductionContext context, Location? location, string currentTag, string availableTags)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DLA002_CurrentLanguageTagIsNotInTheTagList,
            location,
            currentTag,
            availableTags));
    }

    public static void ReportLanguageKeyInconsistent(this SourceProductionContext context, Location? location, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DLA003_LanguageKeyInconsistent,
            location,
            message));
    }

    public static void ReportInvalidConfigurationCombination(this SourceProductionContext context, Location? location)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DLA004_InvalidConfigurationCombination,
            location));
    }

    public static void ReportCompiledModeRequiresEnsureKeysIdentical(this SourceProductionContext context, Location? location)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DLA005_CompiledModeRequiresEnsureKeysIdentical,
            location));
    }
}
