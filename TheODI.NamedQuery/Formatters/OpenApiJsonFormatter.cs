using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.OpenApi;
using System.Text;

namespace TheODI.NamedQuery.Formatters;

internal class OpenApiJsonFormatter : TextOutputFormatter
{
    internal const string MediaType = "application/openapi+json; version=3.2";

    public OpenApiJsonFormatter()
    {
        SupportedMediaTypes.Add(MediaType);
        SupportedEncodings.Add(Encoding.UTF8);
    }

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding _) =>
        ((OpenApiDocument)context.Object!).SerializeAsync(
            context.HttpContext.Response.Body,
            OpenApiSpecVersion.OpenApi3_2,
            OpenApiConstants.Json,
            context.HttpContext.RequestAborted);

    protected override bool CanWriteType(Type? type) => typeof(OpenApiDocument).IsAssignableFrom(type);
}
