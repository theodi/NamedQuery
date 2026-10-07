using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace DockerTest;

[TestClass]
public sealed class DockerBuildTests
{
    private const ushort Port = 8080;

    public required TestContext TestContext { get; set; }

    [TestMethod]
    public async Task ValidContextIsServed()
    {
        await using var compose = Compose("valid")
            .WithExposedService("valid", Port, Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(Port)))
            .Build();

        await compose.StartAsync(TestContext.CancellationToken);

        using var client = new HttpClient
        {
            BaseAddress = new UriBuilder(Uri.UriSchemeHttp, compose.GetServiceHost("valid", Port), compose.GetServicePort("valid", Port)).Uri
        };

        var response = await client.GetAsync("", TestContext.CancellationToken);

        response.Should().Be200Ok();
        var results = await response.Content.ReadFromJsonAsync<string[]>(TestContext.CancellationToken);
        results.Should().ContainSingle().Which.Should().MatchRegex("\"boolean\"\\s*:\\s*true");
    }

    [TestMethod]
    public async Task InvalidContextFailsBuild()
    {
        await using var compose = Compose("invalid").Build();

        var start = () => compose.StartAsync(TestContext.CancellationToken);

        (await start.Should().ThrowAsync<ExecFailedException>()).Which.Message.Should().Contain("Manifest is not valid Turtle");
    }

    [TestMethod]
    public async Task MissingContextFailsBuild()
    {
        await using var compose = Compose("missing").Build();

        var start = () => compose.StartAsync(TestContext.CancellationToken);

        (await start.Should().ThrowAsync<ExecFailedException>()).Which.Message.Should().Contain("endpoint-definition-folder");
    }

    private static ComposeBuilder Compose(string service) => new ComposeBuilder("docker:29-cli")
        .WithComposeFile(Path.Combine(CommonDirectoryPath.GetProjectDirectory().DirectoryPath, "compose.yml"))
        .WithCopyFilesInContainer("../../Dockerfile", "../../.dockerignore", "../../Directory.Build.props", "../../Directory.Packages.props", "../../.editorconfig", "../../Web", "../../dotNetRDF.Wrapping", "../../ValidateResources", "Fixtures")
        .WithService(service)
        .WithPull(false)
        .WithComposeUpOption("--build")
        .WithComposeDownOption("--rmi", "local");
}
