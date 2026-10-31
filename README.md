# Library

The functionality is published on NuGet as [TheODI.NamedQuery](https://www.nuget.org/packages/TheODI.NamedQuery), for serving your own endpoint definitions from any ASP.NET Core app.

```shell
dotnet add package TheODI.NamedQuery --prerelease
```

```csharp
builder.Services.AddNamedQuery(options => options.Definitions = new ManifestEmbeddedFileProvider(typeof(Program).Assembly));

var app = builder.Build();
app.UseNamedQuery();
```

See the [package README](TheODI.NamedQuery/README.md) for setup, the manifest reference and the routes it maps.

# Build

```shell
docker build --build-context endpoint-definition-folder=ExampleEndpointDefinitions -t named-query .
```

# Run
```shell
docker run --rm -p 8080:8080 -e NamedQuery__SparqlEndpoint=https://example.com/sparql named-query
```

# Run locally

Serves the endpoint definitions in `App/LocalDevelopmentEndpointDefinitions`.

```shell
dotnet run --project App -- --NamedQuery:SparqlEndpoint=https://example.com/sparql
```

# Test

Docker tests require a running Docker daemon.

```shell
dotnet test --solution NamedQuery.slnx
```

# Release

Publishing a GitHub release whose tag is `v` followed by the version (e.g. `v0.1.0-alpha.1`) runs the [release workflow](.github/workflows/release.yml). It tests, packs the library with that version, pushes it to NuGet and attaches the packages to the release. Mark versions with a `-` suffix as pre-releases.

# MCP

Each endpoint is also exposed as a read-only tool on the [MCP](https://modelcontextprotocol.io) server at `/mcp` (Streamable HTTP). Tool names are endpoint paths with characters other than letters, digits, `_` and `-` replaced by `_`.

For example, to connect a client that only supports local servers:

```json
{
  "mcpServers": {
    "named-query": {
      "command": "npx",
      "args": ["mcp-remote", "http://localhost:8080/mcp"]
    }
  }
}
```
