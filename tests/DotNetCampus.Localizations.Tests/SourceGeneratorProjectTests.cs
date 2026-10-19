namespace DotNetCampus.Localizations.Tests;

[TestClass]
public class SourceGeneratorProjectTests
{
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