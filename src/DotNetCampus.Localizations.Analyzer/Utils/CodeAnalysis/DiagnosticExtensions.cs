using Microsoft.CodeAnalysis;

namespace dotnetCampus.Localizations.Utils.CodeAnalysis;

public static class DiagnosticExtensions
{
    public static void ReportUnknownError(this SourceProductionContext context, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DL0000_UnknownError,
            null,
            message));
    }

    public static void ReportLanguageKeyInconsistent(this SourceProductionContext context, string message)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Diagnostics.DL0003_LanguageKeyInconsistent,
            null,
            message));
    }
}
