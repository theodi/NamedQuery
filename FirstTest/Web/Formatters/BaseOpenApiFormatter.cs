using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.OpenApi;
using System.Text;

namespace Web.Formatters;

internal abstract class BaseOpenApiFormatter : TextOutputFormatter
{
    private readonly string format;

    protected BaseOpenApiFormatter(string mediaType, string format)
    {
        this.format = format;

        SupportedMediaTypes.Add(mediaType);
        SupportedEncodings.Add(Encoding.UTF8);
    }

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding _) =>
        ((OpenApiDocument)context.Object!).SerializeAsync(
            context.HttpContext.Response.Body,
            OpenApiSpecVersion.OpenApi3_2,
            format,
            context.HttpContext.RequestAborted);

    protected override bool CanWriteType(Type? type) => type!.IsAssignableFrom(typeof(OpenApiDocument));
}
