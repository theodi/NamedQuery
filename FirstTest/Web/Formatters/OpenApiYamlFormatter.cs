using Microsoft.OpenApi;

namespace Web.Formatters;

internal class OpenApiYamlFormatter() : OpenApiFormatterBase(MediaType, OpenApiConstants.Yaml)
{
    internal const string MediaType = "application/openapi+yaml; version=3.2";
}
