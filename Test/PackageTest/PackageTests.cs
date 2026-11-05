namespace PackageTest;

[TestClass]
public sealed class PackageTests
{
    private const string Library = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="TheODI.NamedQuery" Version="{0}" />
          </ItemGroup>
        </Project>
        """;

    private const string TestProject = """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <IsTestProject>true</IsTestProject>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="TheODI.NamedQuery" Version="{0}" />
          </ItemGroup>
        </Project>
        """;

    private const string WebProject = """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="TheODI.NamedQuery" Version="{0}" />
          </ItemGroup>
        </Project>
        """;

    [TestMethod]
    public void EmbeddedDefinitionsWithRootFolderBuild()
    {
        var consumer = Consumer.Embedded(nameof(EmbeddedDefinitionsWithRootFolderBuild));

        var first = consumer.Build();
        first.ExitCode.Should().Be(0, first.Output);
        first.Validated.Should().BeTrue(first.Output);

        var published = consumer.Publish();
        published.ExitCode.Should().Be(0, published.Output);
    }

    [TestMethod]
    public void HostControllerCollides()
    {
        const string controller = """

            [Microsoft.AspNetCore.Mvc.ApiController]
            public class ThingsController : Microsoft.AspNetCore.Mvc.ControllerBase
            {
                [Microsoft.AspNetCore.Mvc.HttpGet("things")]
                public string Get() => "host";
            }
            """;

        AssertCollision(Consumer.Embedded(nameof(HostControllerCollides), "app.MapControllers();", controller));
    }

    [TestMethod]
    public void HostMapGetCollides()
    {
        AssertCollision(Consumer.Embedded(nameof(HostMapGetCollides), "app.MapGet(\"/things\", () => \"host\");"));
    }

    [TestMethod]
    public void HostPostOnlyRouteDoesNotCollide()
    {
        var build = Consumer.Embedded(nameof(HostPostOnlyRouteDoesNotCollide), "app.MapPost(\"/things\", () => \"host\");").Build();

        build.ExitCode.Should().Be(0, build.Output);
        build.Validated.Should().BeTrue(build.Output);
    }

    [TestMethod]
    public void ThrowingStartupIsReported()
    {
        var build = Consumer.Create(nameof(ThrowingStartupIsReported), WebProject, "throw new InvalidOperationException(\"boom\");").Build();

        build.ExitCode.Should().NotBe(0, build.Output);
        build.Output.Should().Contain("error NQ0002");
    }

    [TestMethod]
    public void ClassLibraryIsSkipped()
    {
        var build = Consumer.Create(nameof(ClassLibraryIsSkipped), Library, "public static class Nothing;").Build();

        build.ExitCode.Should().Be(0, build.Output);
        build.Validated.Should().BeFalse(build.Output);
    }

    [TestMethod]
    public void AppReferencingNamedQueryOnlyThroughProjectIsNotValidated()
    {
        var consumer = Consumer.Create(nameof(AppReferencingNamedQueryOnlyThroughProjectIsNotValidated), """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="Library\Library.csproj" />
              </ItemGroup>
            </Project>
            """, "throw new InvalidOperationException(\"boom\");", new Dictionary<string, string>
        {
            [Path.Combine("Library", "Library.csproj")] = Library.Replace("{0}", PackageFeed.Version),
            [Path.Combine("Library", "Nothing.cs")] = "public static class Nothing;"
        });

        var build = consumer.Build();

        build.ExitCode.Should().Be(0, build.Output);
        build.Validated.Should().BeFalse(build.Output);
    }

    [TestMethod]
    public void TestProjectIsSkipped()
    {
        var build = Consumer.Create(nameof(TestProjectIsSkipped), TestProject, "throw new System.InvalidOperationException(\"boom\");").Build();

        build.ExitCode.Should().Be(0, build.Output);
        build.Validated.Should().BeFalse(build.Output);
    }

    [TestMethod]
    public void AppWithoutNamedQueryIsQuiet()
    {
        var build = Consumer.Create(nameof(AppWithoutNamedQueryIsQuiet), WebProject, """
            var app = WebApplication.CreateBuilder(args).Build();
            app.MapGet("/", () => "host");
            app.Run();
            """).Build();

        build.ExitCode.Should().Be(0, build.Output);
        build.Validated.Should().BeTrue(build.Output);
        build.Output.Should().NotContain(": error").And.NotContain(": warning NQ");
    }

    private static void AssertCollision(Consumer consumer)
    {
        var build = consumer.Build();

        build.ExitCode.Should().NotBe(0, build.Output);
        build.Output.Should().Contain("manifest.ttl(7,15,7,23): error NQ1006");
    }
}
