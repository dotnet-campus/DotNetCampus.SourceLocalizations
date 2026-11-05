namespace DotNetCampus.Localizations.Tests;

/// <summary>
/// 通过实际编译和运行验证 Key 比较配置、跨语言一致性以及专用错误诊断。
/// </summary>
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
    public async Task WhenCaseInsensitiveKeysConflictThenBuildReportsSpecificDiagnostic(
        string scenario, string mode, bool ensureKeysIdentical, (string Path, string Content)[] files, string[] expected)
    {
        var result = await ExecuteAsync(scenario, mode, ensureKeysIdentical, files, isKeysCaseSensitive: false);

        Assert.AreNotEqual(0, result.ExitCode, result.Output);
        var diagnosticId = scenario is "CaseConflictAcrossLanguages" or "NestedConflictAcrossLanguages" ? "DLA008" : "DLA007";
        StringAssert.Contains(result.Output, diagnosticId);
        Assert.IsFalse(result.Output.Contains("DLA000"), result.Output);
        foreach (var text in expected)
        {
            StringAssert.Contains(result.Output, text);
        }
    }

    /// <summary>
    /// 默认配置和显式启用大小写敏感时，强类型成员、分组及字典均应保留大小写不同的 Key。
    /// </summary>
    [TestMethod]
    [DataRow("Dictionary", null, "Library")]
    [DataRow("Dictionary", true, "Library")]
    [DataRow("Compiled", null, "Library")]
    [DataRow("Compiled", true, "Library")]
    [DataRow("Dictionary", null, "NestedSource")]
    [DataRow("Compiled", true, "NestedSource")]
    public async Task WhenKeysAreCaseSensitiveThenDistinctValuesSurviveLanguageChanges(string mode, bool? isKeysCaseSensitive, string dependencyMode)
    {
        var assertions = """
            AssertEqual("en mixed", Lang.Current.Abc.ToString());
            AssertEqual("en upper", Lang.Current.ABC.ToString());
            AssertEqual("en nested mixed", Lang.Current.Menu.Abc.ToString());
            AssertEqual("en nested upper", Lang.Current.Menu.ABC.ToString());
            AssertEqual("en lower group", Lang.Current.menu.Abc.ToString());
            Lang.SetCurrent("zh-Hans");
            AssertEqual("zh mixed", Lang.Current.Abc.ToString());
            AssertEqual("zh upper", Lang.Current.ABC.ToString());
            AssertEqual("zh nested mixed", Lang.Current.Menu.Abc.ToString());
            AssertEqual("zh nested upper", Lang.Current.Menu.ABC.ToString());
            AssertEqual("zh lower group", Lang.Current.menu.Abc.ToString());
            """;
        if (mode == "Dictionary")
        {
            assertions += """

                AssertEqual("zh mixed", Lang.Current["Abc"]);
                AssertEqual("zh upper", Lang.Current["ABC"]);
                AssertEqual("", Lang.Current["abc"]);
                """;
        }

        var result = await ExecuteAsync("DistinctCaseValues", mode, true,
            [("en.toml", "Abc = \"en mixed\"\nMenu.Abc = \"en nested mixed\"\nmenu.Abc = \"en lower group\""),
             ("en.yaml", "ABC: en upper\nMenu.ABC: en nested upper"),
             ("zh-Hans.yaml", "Abc: zh mixed\nMenu.Abc: zh nested mixed\nmenu.Abc: zh lower group"),
             ("zh-Hans.toml", "ABC = \"zh upper\"\nMenu.ABC = \"zh nested upper\"")],
            isKeysCaseSensitive, assertions, dependencyMode);

        Assert.AreEqual(0, result.ExitCode, result.Output);
    }

    /// <summary>
    /// 忽略大小写的配置还必须影响运行时查询及默认语言回退，避免只放宽编译检查。
    /// </summary>
    [TestMethod]
    [DataRow("Library")]
    [DataRow("NestedSource")]
    public async Task WhenKeysAreCaseInsensitiveThenDictionaryLookupAndFallbackIgnoreCase(string dependencyMode)
    {
        var result = await ExecuteAsync("InsensitiveLookup", "Dictionary", false,
            [("en.toml", "Abc = \"english\"\nOnlyDefault = \"fallback\"\nMenu.Abc = \"nested english\""),
             ("zh-Hans.yaml", "Abc: chinese\nMenu.Abc: nested chinese")],
            false, """
                AssertEqual("english", Lang.Current["ABC"]);
                AssertEqual("nested english", Lang.Current["MENU.ABC"]);
                Lang.SetCurrent("zh-Hans");
                AssertEqual("chinese", Lang.Current["abc"]);
                AssertEqual("chinese", Lang.Current.Abc.ToString());
                AssertEqual("nested chinese", Lang.Current.Menu.Abc.ToString());
                AssertEqual("fallback", Lang.Current["ONLYDEFAULT"]);
                """, dependencyMode);

        Assert.AreEqual(0, result.ExitCode, result.Output);
    }

    /// <summary>
    /// 完全相同的 Key 在任一大小写配置下都不能重复定义，包括跨资源格式的重复。
    /// </summary>
    [TestMethod]
    [DataRow("Dictionary", null)]
    [DataRow("Dictionary", true)]
    [DataRow("Compiled", null)]
    [DataRow("Compiled", true)]
    public async Task WhenExactKeysAreDuplicatedThenBuildReportsDla007(string mode, bool? isKeysCaseSensitive)
    {
        var result = await ExecuteAsync("ExactDuplicates", mode, false,
            [("en.toml", "Abc = \"first\""), ("en.yaml", "Abc: second")], isKeysCaseSensitive);

        Assert.AreNotEqual(0, result.ExitCode, result.Output);
        StringAssert.Contains(result.Output, "DLA007");
        Assert.IsFalse(result.Output.Contains("DLA000"), result.Output);
    }

    /// <summary>
    /// 大小写敏感时，跨语言的大小写差异属于键集合差异，由 EnsureKeysIdentical 决定是否报错。
    /// </summary>
    [TestMethod]
    [DataRow("Dictionary", false)]
    [DataRow("Dictionary", true)]
    [DataRow("Compiled", false)]
    [DataRow("Compiled", true)]
    public async Task WhenCaseSensitiveLanguagesUseDifferentCasingThenEnsureKeysIdenticalControlsError(string mode, bool ensureKeysIdentical)
    {
        var result = await BuildAsync("DistinctCaseAcrossLanguages", mode, ensureKeysIdentical,
            ("en.toml", "Abc = \"english\""), ("zh-Hans.yaml", "ABC: chinese"));

        Assert.AreEqual(ensureKeysIdentical, result.ExitCode != 0, result.Output);
        if (ensureKeysIdentical)
        {
            StringAssert.Contains(result.Output, "DLA003");
        }
        Assert.IsFalse(result.Output.Contains("DLA008") || result.Output.Contains("DLA000"), result.Output);
    }

    [TestMethod]
    [DataRow("Dictionary", false, null)]
    [DataRow("Dictionary", true, null)]
    [DataRow("Compiled", false, null)]
    [DataRow("Compiled", true, null)]
    [DataRow("Dictionary", false, false)]
    [DataRow("Dictionary", true, false)]
    [DataRow("Compiled", false, false)]
    [DataRow("Compiled", true, false)]
    public async Task WhenKeysMatchAcrossLanguagesThenBuildSucceeds(string mode, bool ensureKeysIdentical, bool? isKeysCaseSensitive)
    {
        var result = await ExecuteAsync("MatchingKeys", mode, ensureKeysIdentical,
            [("en.toml", "Abc = \"english\"\n[Menu]\nAbc = \"nested english\""),
             ("zh-Hans.yaml", "Abc: chinese\nMenu.Abc: nested chinese")], isKeysCaseSensitive);

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

    private static Task<TestProjectResult> BuildAsync(
        string scenario, string mode, bool ensureKeysIdentical, params (string Path, string Content)[] files)
        => ExecuteAsync(scenario, mode, ensureKeysIdentical, files, isKeysCaseSensitive: null);

    /// <summary>
    /// 在独立输入目录中生成配置和资源；有运行时断言时编译运行，否则只检查构建诊断。
    /// </summary>
    /// <param name="scenario">用于隔离资源输入的用例名称。</param>
    /// <param name="mode">Dictionary 或 Compiled 生成模式。</param>
    /// <param name="ensureKeysIdentical">是否要求各语言完整键集合一致。</param>
    /// <param name="files">相对于输入目录的资源路径及内容。</param>
    /// <param name="isKeysCaseSensitive">大小写配置；null 表示省略属性以验证默认值。</param>
    /// <param name="assertions">编译运行的断言代码；null 表示只执行构建。</param>
    /// <param name="dependencyMode">使用库依赖或内嵌源代码。</param>
    /// <returns>构建或运行进程的退出码及输出。</returns>
    private static async Task<TestProjectResult> ExecuteAsync(
        string scenario, string mode, bool ensureKeysIdentical, (string Path, string Content)[] files,
        bool? isKeysCaseSensitive, string? assertions = null, string dependencyMode = "Library")
    {
        var projectPath = TestProjectRunner.GetTestProjectPath("LocalizationKeyValidation", "LocalizationKeyValidation.csproj");
        var inputDirectory = Path.Combine(Path.GetDirectoryName(projectPath)!, ".artifacts", "obj", "inputs", scenario, mode,
            ensureKeysIdentical.ToString(), isKeysCaseSensitive?.ToString() ?? "Default", dependencyMode);
        Directory.CreateDirectory(inputDirectory);
        // 省略属性本身才能验证源生成器对默认值的处理，而不只是显式传入 true。
        var caseConfiguration = isKeysCaseSensitive is { } caseSensitive ? $", IsKeysCaseSensitive = {caseSensitive.ToString().ToLowerInvariant()}" : "";
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "Lang.cs"), $$"""
            using DotNetCampus.Localizations;

            namespace LocalizationKeyValidation;

            [LocalizedConfiguration(Default = "en", Current = "en", GenerationMode = GenerationMode.{{mode}},
                NotificationMode = NotificationMode.CurrentCulturePropertyChanged, DependencyMode = DependencyMode.{{dependencyMode}},
                EnsureKeysIdentical = {{(ensureKeysIdentical ? "true" : "false")}}{{caseConfiguration}})]
            internal static partial class Lang;

            internal static class Program
            {
                public static void Main()
                {
                    {{assertions}}
                }

                private static void AssertEqual(string expected, string actual)
                {
                    if (expected != actual)
                    {
                        throw new System.InvalidOperationException($"Expected: {expected}; Actual: {actual}.");
                    }
                }
            }
            """);
        foreach (var (path, content) in files)
        {
            var filePath = Path.Combine(inputDirectory, path);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.WriteAllTextAsync(filePath, content);
        }

        return assertions is null
            ? await TestProjectRunner.BuildAsync(projectPath, $"-p:ValidationInputDirectory={inputDirectory}")
            : await TestProjectRunner.RunAsync(projectPath, $"-p:ValidationInputDirectory={inputDirectory}");
    }
}
