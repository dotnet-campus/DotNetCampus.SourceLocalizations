using System;
using Microsoft.CodeAnalysis;

namespace DotNetCampus.Localizations.Utils.CodeAnalysis;

/// <summary>
/// 表示源生成器在代码生成过程中遇到的诊断错误。
/// 通过 catch 此异常并调用 <see cref="CreateDiagnostic"/> 即可将错误报告给编译器。
/// </summary>
internal sealed class DiagnosticsException : Exception
{
    /// <summary>
    /// 创建要报告的诊断，并使用格式化后的诊断消息作为异常消息。
    /// </summary>
    /// <param name="descriptor">要报告的诊断描述符。</param>
    /// <param name="location">诊断对应的源位置。没有源位置时可传入 <see langword="null"/>。</param>
    /// <param name="messageArgs">用于格式化诊断消息的参数。</param>
    public DiagnosticsException(DiagnosticDescriptor descriptor, Location? location, params object?[] messageArgs)
        : this(Diagnostic.Create(descriptor, location, messageArgs))
    {
    }

    private DiagnosticsException(Diagnostic diagnostic) : base(diagnostic.GetMessage())
    {
        Diagnostic = diagnostic;
    }

    /// <summary>
    /// 获取供生成器报告的原始诊断。
    /// </summary>
    public Diagnostic Diagnostic { get; }
}
