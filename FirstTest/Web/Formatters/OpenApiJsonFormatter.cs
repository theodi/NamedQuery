using Microsoft.OpenApi;

namespace Web.Formatters;

internal class OpenApiJsonFormatter() : BaseOpenApiFormatter(MediaType, OpenApiConstants.Json)
{
    internal const string MediaType = "application/openapi+json; version=3.2";
}
