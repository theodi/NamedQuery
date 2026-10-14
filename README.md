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
