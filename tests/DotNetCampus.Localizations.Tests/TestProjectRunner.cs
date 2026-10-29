using System.Diagnostics;
using System.Text;

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
    /// <param name="projectPath">测试项目的完整路径。</param>
    /// <param name="additionalArguments">传递给 dotnet 的额外构建参数，用于选择当前测试生成的资源输入。</param>
    public static Task<TestProjectResult> RunAsync(string projectPath, params string[] additionalArguments)
    {
        return ExecuteAsync("run", projectPath, additionalArguments);
    }

    /// <summary>
    /// 构建指定测试项目。
    /// </summary>
    public static Task<TestProjectResult> BuildAsync(string projectPath, params string[] additionalArguments)
    {
        return ExecuteAsync("build", projectPath, additionalArguments);
    }

    private static async Task<TestProjectResult> ExecuteAsync(string command, string projectPath, params string[] additionalArguments)
    {
        var projectDirectory = Path.GetDirectoryName(projectPath)!;
        var artifactsPath = Path.Combine(projectDirectory, ".artifacts");
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = projectDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // dotnet 输出 UTF-8；显式解码，避免中文诊断旁的 Key 字母被系统代码页吞入乱码。
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.StartInfo.ArgumentList.Add(command);
        if (command == "run")
        {
            process.StartInfo.ArgumentList.Add("--project");
        }
        process.StartInfo.ArgumentList.Add(projectPath);
        process.StartInfo.ArgumentList.Add("--configuration");
        process.StartInfo.ArgumentList.Add("Debug");
        process.StartInfo.ArgumentList.Add($"-p:ArtifactsPath={artifactsPath}");
        foreach (var argument in additionalArguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

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
