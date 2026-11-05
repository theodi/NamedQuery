# Build-time validation

Endpoint definitions are validated when the app is built, so mistakes fail the build instead of surfacing as errors at request time.

## Setup

None. Any executable project (not a test project or class library) that references `TheODI.NamedQuery` directly is validated after `Build`, through a `build` targets file in the package. Projects that only reference the package indirectly, through another project, are not validated. Design-time builds are skipped.

Set `NamedQueryValidateDefinitions` to `true` or `false` in the `.csproj` to opt in or out:

```xml
<PropertyGroup>
  <NamedQueryValidateDefinitions>false</NamedQueryValidateDefinitions>
</PropertyGroup>
```

## How it works

After `Build`, the built app is run with a [startup hook](https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md) from `TheODI.NamedQuery` itself. The hook builds and starts the host with a no-op server, then reads the definitions the app configured and all of its endpoints: controllers, Razor Pages, minimal APIs, reverse proxies and the NamedQuery built-ins. Validation is skipped with a warning (NQ0003) if `StartupHookSupport` is `false`.

Your startup code and hosted services run. To skip side effects such as migrations or outbound calls during validation, check the `NAMEDQUERY_VALIDATION` environment variable:

```csharp
if (Environment.GetEnvironmentVariable("NAMEDQUERY_VALIDATION") == "1")
{
    // validation run
}
```

If no `SparqlEndpoint` is configured, a placeholder one is used.

## Diagnostics

Errors are MSBuild diagnostics (`path(line,col): error NQxxxx: message`). They show in `dotnet build` and in the Visual Studio Error List, and fail the build.

| Code | Meaning |
|---|---|
| NQ0001 | The validation run exited unexpectedly |
| NQ0002 | The host failed to build or start, or timed out |
| NQ0003 | Validation skipped because `StartupHookSupport` is `false` (warning) |
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

## Properties

| Property | Default | Meaning |
|---|---|---|
| `NamedQueryValidateDefinitions` | automatic | `true` or `false` to force validation on or off |
| `NamedQueryValidationEnvironment` | | Sets `ASPNETCORE_ENVIRONMENT` for the validation run |
| `NamedQueryValidationTimeout` | `60` | Seconds to wait for the host before reporting NQ0002 |
