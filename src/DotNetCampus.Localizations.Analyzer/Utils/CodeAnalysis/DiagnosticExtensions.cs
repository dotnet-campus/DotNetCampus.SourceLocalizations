using Microsoft.CodeAnalysis;

namespace DotNetCampus.Localizations.Utils.CodeAnalysis;

public static class DiagnosticExtensions
{
    public static void ReportUnknownError(this SourceProductionContext context, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DL0000_UnknownError,
            null,
            message));
    }

    public static void ReportDefaultLanguageTagNotFound(this SourceProductionContext context, string defaultTag, string availableTags)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DL0001_DefaultLanguageTagIsNotInTheTagList,
            null,
            defaultTag,
            availableTags));
    }

    public static void ReportLanguageKeyInconsistent(this SourceProductionContext context, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DL0003_LanguageKeyInconsistent,
            null,
            message));
    }

    public static void ReportInvalidConfigurationCombination(this SourceProductionContext context)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DL0004_InvalidConfigurationCombination,
            null));
    }
}
