# Build

```shell
docker build --build-context endpoint-definition-folder=ExampleEndpointDefinitions -t named-query-first-test .
```

# Run
```shell
docker run --rm -p 8080:8080 -e Options__SparqlEndpoint=https://example.com/sparql named-query-first-test
```

# Test

Docker tests require a running Docker daemon.

```shell
dotnet test --solution FirstTest.slnx
```
