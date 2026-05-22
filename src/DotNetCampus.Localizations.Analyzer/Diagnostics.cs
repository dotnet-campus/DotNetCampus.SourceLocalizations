using Microsoft.CodeAnalysis;
using static dotnetCampus.Localizations.Properties.Localizations;

// ReSharper disable InconsistentNaming

namespace dotnetCampus.Localizations;

/// <summary>
/// 包含日志库中的所有诊断。
/// </summary>
public class Diagnostics
{
    public static DiagnosticDescriptor DL0000_UnknownError { get; } = new(
        nameof(DLA000),
        Localize(nameof(DLA000)),
        Localize(nameof(DLA000_Message)),
        Categories.Useless,
        DiagnosticSeverity.Error,
        true);

    public static DiagnosticDescriptor DL0001_DefaultLanguageTagIsNotInTheTagList { get; } = new(
        nameof(DLA001),
        Localize(nameof(DLA001)),
        Localize(nameof(DLA001_Message)),
        Categories.RuntimeException,
        DiagnosticSeverity.Error,
        true);

    public static DiagnosticDescriptor DL0002_CurrentLanguageTagIsNotInTheTagList { get; } = new(
        nameof(DLA002),
        Localize(nameof(DLA002)),
        Localize(nameof(DLA002_Message)),
        Categories.RuntimeException,
        DiagnosticSeverity.Error,
        true);

    public static DiagnosticDescriptor DL0003_LanguageKeyInconsistent { get; } = new(
        nameof(DLA003),
        Localize(nameof(DLA003)),
        Localize(nameof(DLA003_Message)),
        Categories.RuntimeException,
        DiagnosticSeverity.Error,
        true);

    private static class Categories
    {
        /// <summary>
        /// 可能产生 bug，则报告此诊断。
        /// </summary>
        public const string AvoidBugs = "dotnetCampus.AvoidBugs";

        /// <summary>
        /// 为了提供代码生成能力，则报告此诊断。
        /// </summary>
        public const string CodeFixOnly = "dotnetCampus.CodeFixOnly";

        /// <summary>
        /// 因编译要求而必须满足的条件没有满足，则报告此诊断。
        /// </summary>
        public const string Compiler = "dotnetCampus.Compiler";

        /// <summary>
        /// 因库内的机制限制，必须满足此要求后库才可正常工作，则报告此诊断。
        /// </summary>
        public const string Mechanism = "dotnetCampus.Mechanism";

        /// <summary>
        /// 为了代码可读性，使之更易于理解、方便调试，则报告此诊断。
        /// </summary>
        public const string Readable = "dotnetCampus.Readable";

        /// <summary>
        /// 为了提升性能，或避免性能问题，则报告此诊断。
        /// </summary>
        public const string Performance = "dotnetCampus.Performance";

        /// <summary>
        /// 能写得出来正常编译，但会引发运行时异常，则报告此诊断。
        /// </summary>
        public const string RuntimeException = "dotnetCampus.RuntimeException";

        /// <summary>
        /// 编写了无法生效的代码，则报告此诊断。
        /// </summary>
        public const string Useless = "dotnetCampus.Useless";
    }

    private static LocalizableString Localize(string key) => new LocalizableResourceString(key, ResourceManager, typeof(Properties.Localizations));
}
