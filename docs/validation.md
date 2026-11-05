# Build-time validation

Endpoint definitions are validated when the app is built, so mistakes fail the build instead of surfacing as errors at request time.

## Setup

None. Every project that references `TheODI.NamedQuery` directly is validated after `Build`, through a `build` targets file in the package, by running the built project. Reference the package only from the web app project, not from class libraries or test projects: a library cannot be run, so its build fails. Projects that get the package transitively, through another project, are not validated.

Validation cannot be turned off.

## How it works

After `Build`, the built app is run with a [startup hook](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md) from `TheODI.NamedQuery` itself. The hook builds and starts the host with a no-op server, then reads the definitions the app configured and all of its endpoints: controllers, Razor Pages, minimal APIs, reverse proxies and the NamedQuery built-ins.

Your startup code and hosted services run. If they throw, the unhandled exception is printed and the build fails with the app's non-zero exit code. To skip side effects such as migrations or outbound calls during validation, check the `NAMEDQUERY_VALIDATION` environment variable:

```csharp
if (Environment.GetEnvironmentVariable("NAMEDQUERY_VALIDATION") == "1")
{
    // validation run
}
```

Validation sets the `NamedQuery__SparqlEndpoint` environment variable to a placeholder, so apps that configure the endpoint through configuration do not need one at build time. A `SparqlEndpoint` set in code is not overridden.

## Diagnostics

Errors are MSBuild diagnostics (`path(line,col): error NQxxxx: message`). They show in `dotnet build` and in the Visual Studio Error List, and fail the build. Paths are relative to the definitions root, for example `manifest.ttl(7,15,7,23)`.

| Code | Meaning |
|---|---|
| NQ0002 | The app exited before the host started, or validation itself failed |
| NQ1000 | Internal error |
| NQ1001 | Manifest missing |
| NQ1002 | Turtle syntax error |
| NQ1003 | SHACL shape violation |
| NQ1004 | SPARQL file missing |
| NQ1005 | SPARQL syntax error |
| NQ1006 | Endpoint path collides with an existing `GET`/`HEAD` route |
| NQ1007 | Parameter collides with the `format` query parameter |
| NQ1008 | MCP tool name collision |
| NQ1009 | Frame file missing |
| NQ1010 | Frame is not valid JSON |
