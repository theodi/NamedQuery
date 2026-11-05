using System.Diagnostics;

namespace PackageTest;

[TestClass]
public static class PackageFeed
{
    internal static string Version { get; } = $"0.0.0-test.{Guid.NewGuid():N}";

    internal static string Root { get; private set; } = null!;

    internal static string Feed => Path.Combine(Root, "feed");

    [AssemblyInitialize]
    public static void Pack(TestContext context)
    {
        Root = Directory.CreateTempSubdirectory("NamedQueryPackageTest").FullName;
        File.WriteAllText(Path.Combine(Root, "nuget.config"), $"""
            <configuration>
              <packageSources>
                <clear />
                <add key="feed" value="{Feed}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <packageSourceMapping>
                <packageSource key="feed">
                  <package pattern="TheODI.NamedQuery" />
                </packageSource>
                <packageSource key="nuget.org">
                  <package pattern="*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """);

        var result = Dotnet(Root, "pack", Path.Combine(RepositoryRoot(), "TheODI.NamedQuery"), "--output", Feed, $"-p:Version={Version}");
        result.ExitCode.Should().Be(0, result.Output);
    }

    [AssemblyCleanup]
    public static void Clean()
    {
        var packages = Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } configured
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        Delete(Path.Combine(packages, "theodi.namedquery", Version.ToLowerInvariant()));

        Delete(Root);
    }

    internal static (int ExitCode, string Output) Dotnet(string workingDirectory, params string[] arguments)
    {
        var info = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        foreach (var argument in arguments.Concat(["-nologo", "-nodeReuse:false"]))
        {
            info.ArgumentList.Add(argument);
        }

        foreach (var key in info.Environment.Keys.Where(key => key.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)).ToList())
        {
            info.Environment.Remove(key);
        }

        info.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        info.Environment["DOTNET_NOLOGO"] = "1";

        using var process = Process.Start(info)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, output + error.Result);
    }

    private static void Delete(string path)
    {
        try
        {
            Directory.Delete(path, true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "NamedQuery.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
