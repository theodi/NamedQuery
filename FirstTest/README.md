# Build

```shell
docker build --build-context endpoint-definition-folder=exampleResources -t named-query-first-test .
```

# Run
```shell
docker run --rm -p 8080:8080 named-query-first-test
```
