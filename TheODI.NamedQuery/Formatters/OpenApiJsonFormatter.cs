using Microsoft.OpenApi;

namespace TheODI.NamedQuery.Formatters;

internal class OpenApiJsonFormatter() : OpenApiFormatterBase(MediaType, OpenApiConstants.Json)
{
    internal const string MediaType = "application/openapi+json; version=3.2";
}
