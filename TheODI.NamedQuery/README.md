# TheODI.NamedQuery

Serve SPARQL queries as named HTTP endpoints from an ASP.NET Core app. You describe each endpoint (path, SPARQL query, parameters) in an RDF manifest, and the library:

- runs the query against your SPARQL endpoint and returns the results with content negotiation (SPARQL results for `SELECT`/`ASK`, RDF graphs for `CONSTRUCT`/`DESCRIBE`)
- publishes an OpenAPI description and Swagger UI
- exposes every endpoint as a read-only tool on an MCP server
- serves a home page that lists the endpoints and a SPARQL query editor

## Install

```shell
dotnet add package TheODI.NamedQuery --prerelease
```

Requires .NET 10.

## Quick start

### 1. Write the endpoint definitions

Create a folder, e.g. `EndpointDefinitions`, with a `manifest.ttl` and the query files it refers to.

`EndpointDefinitions/manifest.ttl`:

```turtle
PREFIX : <http://example.org/namedquery/>
PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>

[
    :name "Example endpoints" ;
    :endpoint [
        :path "triples-by-object" ;
        :sparql "./triples-by-object.sparql"^^xsd:anyURI ;
        :name "Triples by object" ;
        :description "Selects triples whose object is the supplied value." ;
        :parameter [
            :name "value" ;
            :description "Literal object to match." ;
            :example "example" ;
        ] ;
    ] ;
] .
```

`EndpointDefinitions/triples-by-object.sparql`:

```sparql
SELECT * {
    BIND($value AS ?o)
    ?s ?p ?o .
}
```

### 2. Embed the definitions in your app

```shell
dotnet add package Microsoft.Extensions.FileProviders.Embedded
```

In your `.csproj`:

```xml
<PropertyGroup>
  <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
</PropertyGroup>

<ItemGroup>
  <EmbeddedResource Include="EndpointDefinitions\**\*.*" TargetPath="%(RecursiveDir)%(Filename)%(Extension)" />
</ItemGroup>
```

To read the definitions from disk at runtime instead, use `new PhysicalFileProvider(path)` in step 3.

### 3. Register and map

`Program.cs`:

```csharp
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddNamedQuery(options => options.Definitions = new ManifestEmbeddedFileProvider(typeof(Program).Assembly));

var app = builder.Build();
app.UseNamedQuery();

app.Run();
```

### 4. Configure the SPARQL endpoint

`appsettings.json`:

```json
{
  "NamedQuery": {
    "SparqlEndpoint": "https://example.com/sparql"
  }
}
```

Or set the environment variable `NamedQuery__SparqlEndpoint`, or pass `--NamedQuery:SparqlEndpoint=https://example.com/sparql`.

### 5. Call it

```http
GET /triples-by-object?value=example
Accept: application/sparql-results+json
```

or pick the format in the URL: `/triples-by-object?value=example&format=csv`.

## Manifest reference

Vocabulary: `http://example.org/namedquery/`. The manifest has exactly one node with:

| Property | Required | Meaning |
|---|---|---|
| `:name` | yes | Title of the endpoint set (home page, OpenAPI title) |
| `:endpoint` | one or more | The endpoints |

Each endpoint:

| Property | Required | Meaning |
|---|---|---|
| `:path` | yes | Unique URL path, no leading slash; may contain `/` |
| `:sparql` | yes | Relative path of the query file, an `xsd:anyURI` starting with `./` |
| `:name` | yes | Title (home page, OpenAPI, MCP tool title) |
| `:description` | yes | Description (home page, OpenAPI, MCP tool description) |
| `:frame` | no | Relative path of a JSON-LD frame applied to graph results served as JSON-LD |
| `:parameter` | no, repeatable | A query parameter |

Each parameter:

| Property | Required | Meaning |
|---|---|---|
| `:name` | yes | Query string parameter name and SPARQL variable name |
| `:description` | yes | Description (OpenAPI, MCP) |
| `:example` | yes | Example value (home page links, OpenAPI, MCP) |
| `:datatype` | no | Datatype of the bound literal; default `xsd:string` |

Every parameter must be supplied exactly once; otherwise the response is `400 Bad Request`. `format` is reserved.

## What gets mapped

| Route | |
|---|---|
| `/` | Home page listing the endpoints with example links |
| `/sparql` | SPARQL query editor for the configured endpoint |
| `/swagger` | Swagger UI |
| `/openapi.json`, `/openapi.yaml` | OpenAPI description |
| `/mcp` | MCP server (Streamable HTTP); one read-only tool per endpoint |
| `/{path}` | Each endpoint, `GET` only |

Response formats are negotiated from `Accept`, or chosen with `format=csv|jsonld|ttl|rdf|nt|html`. SPARQL endpoint failures become `502` and timeouts `504`, with problem details.

## Known limitations (0.x)

- The library owns the routes above, so endpoint paths must not collide with them, and the host can't have its own `/` page or Swagger UI.
- `AddNamedQuery` changes application-wide MVC options (output formatters, `RespectBrowserAcceptHeader`, `ReturnHttpNotAcceptable`) and registers a default CORS policy that allows any origin.
- Definitions aren't validated by the package. Mistakes surface as errors at request time.

## Links

- Documentation and API reference: https://theodi.github.io/NamedQuery/
- Source, Docker image and issues: https://github.com/theodi/NamedQuery
- Licence: MIT
