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

    public static void ReportDefaultLanguageTagNotFound(this SourceProductionContext context, string defaultTag, string availableTags)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DLA001_DefaultLanguageTagIsNotInTheTagList,
            null,
            defaultTag,
            availableTags));
    }

    public static void ReportCurrentLanguageTagNotFound(this SourceProductionContext context, string currentTag, string availableTags)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DLA002_CurrentLanguageTagIsNotInTheTagList,
            null,
            currentTag,
            availableTags));
    }

    public static void ReportLanguageKeyInconsistent(this SourceProductionContext context, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DLA003_LanguageKeyInconsistent,
            null,
            message));
    }

    public static void ReportInvalidConfigurationCombination(this SourceProductionContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DLA004_InvalidConfigurationCombination,
            null));
    }
}
