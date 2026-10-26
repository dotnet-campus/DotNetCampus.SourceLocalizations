namespace DotNetCampus.Localizations.Tests;

[TestClass]
public class LocalizationKeyValidationTests
{
    public static IEnumerable<object[]> ConflictingKeyCases
    {
        get
        {
            (string Name, (string Path, string Content)[] Files, string[] Expected)[] cases =
            [
                ("TomlCaseConflict", [("en.toml", "Abc = \"first\"\nABC = \"second\"")], ["en", "Abc", "ABC"]),
                ("YamlCaseConflict", [("en.yaml", "Abc: first\nABC: second")], ["en", "Abc", "ABC"]),
                ("DuplicateAcrossTomlFiles", [("First/en.toml", "Abc = \"first\""), ("Second/en.toml", "Abc = \"second\"")], ["en", "Abc"]),
                ("DuplicateAcrossFormats", [("en.toml", "Abc = \"first\""), ("en.yaml", "Abc: second")], ["en", "Abc"]),
                ("CaseConflictAcrossFiles", [("en.toml", "Abc = \"first\""), ("en.yaml", "ABC: second")], ["en", "Abc", "ABC"]),
                ("CaseConflictAcrossLanguages", [("en.toml", "Abc = \"english\""), ("zh-Hans.toml", "ABC = \"chinese\"")], ["en", "zh-Hans", "Abc", "ABC"]),
                ("NonDefaultLanguageConflict", [("en.toml", "Abc = \"english\""), ("zh-Hans.yaml", "Abc: first\nABC: second")], ["zh-Hans", "Abc", "ABC"]),
                ("NestedTomlConflict", [("en.toml", "[Menu]\nAbc = \"first\"\nABC = \"second\"")], ["en", "Menu.Abc", "Menu.ABC"]),
                ("NestedConflictAcrossLanguages", [("en.toml", "[Menu]\nAbc = \"english\""), ("zh-Hans.yaml", "Menu.ABC: chinese")], ["en", "zh-Hans", "Menu.Abc", "Menu.ABC"]),
            ];

            foreach (var testCase in cases)
            {
                foreach (var mode in new[] { "Dictionary", "Compiled" })
                {
                    foreach (var ensureKeysIdentical in new[] { false, true })
                    {
                        yield return [testCase.Name, mode, ensureKeysIdentical, testCase.Files, testCase.Expected];
                    }
                }
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(ConflictingKeyCases))]
    public async Task WhenKeysConflictThenBuildReportsDla000(
        string scenario, string mode, bool ensureKeysIdentical, (string Path, string Content)[] files, string[] expected)
    {
        var result = await BuildAsync(scenario, mode, ensureKeysIdentical, files);

        Assert.AreNotEqual(0, result.ExitCode, result.Output);
        StringAssert.Contains(result.Output, "DLA000");
        foreach (var text in expected)
        {
            StringAssert.Contains(result.Output, text);
        }
    }

    [TestMethod]
    [DataRow("Dictionary", false)]
    [DataRow("Dictionary", true)]
    [DataRow("Compiled", false)]
    [DataRow("Compiled", true)]
    public async Task WhenKeysMatchAcrossLanguagesThenBuildSucceeds(string mode, bool ensureKeysIdentical)
    {
        var result = await BuildAsync("MatchingKeys", mode, ensureKeysIdentical,
            ("en.toml", "Abc = \"english\"\n[Menu]\nAbc = \"nested english\""),
            ("zh-Hans.yaml", "Abc: chinese\nMenu.Abc: nested chinese"));

        Assert.AreEqual(0, result.ExitCode, result.Output);
    }

    [TestMethod]
    [DataRow("Dictionary", false)]
    [DataRow("Dictionary", true)]
    [DataRow("Compiled", false)]
    [DataRow("Compiled", true)]
    public async Task WhenDifferentKeysAreUsedThenEnsureKeysIdenticalControlsTheError(string mode, bool ensureKeysIdentical)
    {
        var result = await BuildAsync("DifferentKeys", mode, ensureKeysIdentical,
            ("en.toml", "Abc = \"english\""),
            ("zh-Hans.yaml", "Other: chinese"));

        if (ensureKeysIdentical)
        {
            Assert.AreNotEqual(0, result.ExitCode, result.Output);
            StringAssert.Contains(result.Output, "DLA003");
        }
        else
        {
            Assert.AreEqual(0, result.ExitCode, result.Output);
        }
    }

    private static async Task<TestProjectResult> BuildAsync(
        string scenario, string mode, bool ensureKeysIdentical, params (string Path, string Content)[] files)
    {
        var projectPath = TestProjectRunner.GetTestProjectPath("LocalizationKeyValidation", "LocalizationKeyValidation.csproj");
        var inputDirectory = Path.Combine(Path.GetDirectoryName(projectPath)!, ".artifacts", "obj", "inputs", scenario, mode, ensureKeysIdentical.ToString());
        Directory.CreateDirectory(inputDirectory);
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "Lang.cs"), $$"""
            using DotNetCampus.Localizations;

            namespace LocalizationKeyValidation;

            [LocalizedConfiguration(Default = "en", GenerationMode = GenerationMode.{{mode}}, EnsureKeysIdentical = {{(ensureKeysIdentical ? "true" : "false")}})]
            internal static partial class Lang;
            """);
        foreach (var (path, content) in files)
        {
            var filePath = Path.Combine(inputDirectory, path);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, content);
        }

        return await TestProjectRunner.BuildAsync(projectPath, $"-p:ValidationInputDirectory={inputDirectory}");
    }
}
