namespace PackageTest;

internal sealed class Consumer
{
    internal const string Manifest = """
        PREFIX : <http://example.org/namedquery/>
        PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>

        [
            :name "Package test" ;
            :endpoint [
                :path "things" ;
                :name "Things" ;
                :description "Selects a triple." ;
                :sparql "./things.sparql"^^xsd:anyURI ;
            ] ;
        ] .
        """;

    internal const string Query = "SELECT * WHERE { ?s ?p ?o } LIMIT 1";

    internal const string EmbeddedProject = """
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
          </PropertyGroup>
          <ItemGroup>
            <EmbeddedResource Include="Endpoints\**" />
            <PackageReference Include="Microsoft.Extensions.FileProviders.Embedded" Version="10.0.14" />
            <PackageReference Include="TheODI.NamedQuery" Version="{0}" />
          </ItemGroup>
        </Project>
        """;

    internal const string EmbeddedProgram = """
        using Microsoft.Extensions.FileProviders;

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddNamedQuery(options => options.Definitions = new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "Endpoints"));
        {1}
        var app = builder.Build();
        app.UseNamedQuery();
        {0}
        app.Run();
        """;

    private Consumer(string directory) => Directory = directory;

    internal string Directory { get; }

    internal static Consumer Create(string name, string project, string program, IDictionary<string, string>? files = null)
    {
        var directory = Path.Combine(PackageFeed.Root, name);
        System.IO.Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, $"{name}.csproj"), project.Replace("{0}", PackageFeed.Version));
        File.WriteAllText(Path.Combine(directory, "Program.cs"), program);

        foreach (var (path, content) in files ?? new Dictionary<string, string>())
        {
            var full = Path.Combine(directory, path);
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
        }

        return new Consumer(directory);
    }

    internal static Consumer Embedded(string name, string hostCode = "", string extraCode = "", string serviceCode = "", string packages = "", IDictionary<string, string>? files = null) => Create(
        name,
        EmbeddedProject.Replace("</ItemGroup>", packages + "</ItemGroup>"),
        EmbeddedProgram.Replace("{0}", hostCode).Replace("{1}", serviceCode) + extraCode,
        new Dictionary<string, string>(files ?? new Dictionary<string, string>())
        {
            [Path.Combine("Endpoints", "manifest.ttl")] = Manifest,
            [Path.Combine("Endpoints", "things.sparql")] = Query
        });

    internal Build Build() => new(PackageFeed.Dotnet(Directory, "build", "-v:n"));

    internal Build Publish() => new(PackageFeed.Dotnet(Directory, "publish", "-v:n", "-o", Path.Combine(Directory, "publish")));
}

internal sealed record Build((int ExitCode, string Output) Result)
{
    internal int ExitCode => Result.ExitCode;

    internal string Output => Result.Output;

    internal bool Validated => Output.Contains("_NamedQueryValidateDefinitions:", StringComparison.Ordinal);
}
