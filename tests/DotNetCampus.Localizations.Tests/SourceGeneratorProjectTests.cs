namespace DotNetCampus.Localizations.Tests;

/// <summary>
/// 通过构建、运行独立用例项目，验证源生成器的实际输出及编译期诊断。
/// </summary>
[TestClass]
public class SourceGeneratorProjectTests
{
    /// <summary>
    /// 验证 TOML、YAML 中的乱序与重复数字占位符在代码生成后仍按原始索引取值。
    /// </summary>
    /// <remarks>
    /// 用例项目同时验证英文默认语言推导出的参数签名与中文、英文的运行结果，
    /// 因此参数数量、类型顺序或文案中的索引发生回归时均会失败。
    /// </remarks>
    /// <returns>等待用例项目完成构建和运行的异步任务。</returns>
    [TestMethod]
    public async Task WhenTomlAndYamlPlaceholdersAreReorderedOrRepeatedThenGeneratedLangPreservesIndices()
    {
        var projectPath = TestProjectRunner.GetTestProjectPath
        (
            "PlaceholderIndices",
            "PlaceholderIndices.csproj"
        );

        // 由真实编译检查生成方法的参数签名，再由用例内的断言检查格式化结果。
        var result = await TestProjectRunner.RunAsync(projectPath);

        Assert.AreEqual(0, result.ExitCode, result.Output);
    }

    [TestMethod]
    public async Task WhenProvidersHaveDifferentPrioritiesThenGeneratedLangUsesExpectedValues()
    {
        var projectPath = TestProjectRunner.GetTestProjectPath
        (
            "ProviderPriority",
            "ProviderPriority.csproj"
        );

        var result = await TestProjectRunner.RunAsync(projectPath);

        Assert.AreEqual(0, result.ExitCode, result.Output);
    }

    [TestMethod]
    public async Task WhenRegisteredAppProviderChangesLanguageThenLibraryReceivesNotification()
    {
        var projectPath = TestProjectRunner.GetTestProjectPath
        (
            "ProviderLanguageChange",
            "Application",
            "ProviderLanguageChange.Application.csproj"
        );

        var result = await TestProjectRunner.RunAsync(projectPath);

        Assert.AreEqual(0, result.ExitCode, result.Output);
    }

    [TestMethod]
    public async Task WhenCompiledModeAddsProvidersThenBuildReportsDla006()
    {
        var projectPath = TestProjectRunner.GetTestProjectPath
        (
            "CompiledProviderError",
            "CompiledProviderError.csproj"
        );

        var result = await TestProjectRunner.BuildAsync(projectPath);

        StringAssert.Contains(result.Output, "DLA006");
    }
}
