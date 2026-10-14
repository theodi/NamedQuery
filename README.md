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
