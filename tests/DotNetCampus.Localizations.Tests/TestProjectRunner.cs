using System.Diagnostics;

namespace DotNetCampus.Localizations.Tests;

public static class TestProjectRunner
{
    private const string TestProjectSetDirectoryName = "TestProjectSet";

    /// <summary>
    /// 从测试输出目录逐级向上查找测试项目集，并返回指定用例项目路径。
    /// </summary>
    public static string GetTestProjectPath(params string[] pathSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (true)
        {
            var testProjectSetPath = Path.Combine(directory.FullName, TestProjectSetDirectoryName);
            if (Directory.Exists(testProjectSetPath))
            {
                return Path.Combine([testProjectSetPath, .. pathSegments]);
            }

            testProjectSetPath = Path.Combine(directory.FullName, "tests", TestProjectSetDirectoryName);
            if (Directory.Exists(testProjectSetPath))
            {
                return Path.Combine([testProjectSetPath, .. pathSegments]);
            }

            directory = directory.Parent!;
        }
    }

    /// <summary>
    /// 构建并运行指定测试项目。
    /// </summary>
    public static Task<TestProjectResult> RunAsync(string projectPath)
    {
        return ExecuteAsync("run", projectPath);
    }

    /// <summary>
    /// 构建指定测试项目。
    /// </summary>
    public static Task<TestProjectResult> BuildAsync(string projectPath)
    {
        return ExecuteAsync("build", projectPath);
    }

    private static async Task<TestProjectResult> ExecuteAsync(string command, string projectPath)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var artifactsPath = Path.Combine(projectDirectory, ".artifacts");
        var projectArgument = command == "run"
            ? $"--project \"{projectPath}\""
            : $"\"{projectPath}\"";
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"{command} {projectArgument} --configuration Debug -p:ArtifactsPath=\"{artifactsPath}\"",
                WorkingDirectory = projectDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.Start();
        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new TestProjectResult(process.ExitCode, await standardOutputTask, await standardErrorTask);
    }
}

public sealed record TestProjectResult(int ExitCode, string StandardOutput, string StandardError)
{
    public string Output => StandardOutput + Environment.NewLine + StandardError;
}