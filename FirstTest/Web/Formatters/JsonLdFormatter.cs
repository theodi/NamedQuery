using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.JsonLd;
using VDS.RDF.Writing;
using Web.Model;

namespace Web.Formatters;

internal class JsonLdFormatter : TextOutputFormatter
{
    public JsonLdFormatter()
    {
        foreach (var mime in MimeTypesHelper.JsonLd)
        {
            SupportedMediaTypes.Add(mime);
        }

        SupportedEncodings.Add(Encoding.UTF8);
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        using var streamWriter = new StreamWriter(context.HttpContext.Response.Body, selectedEncoding);

        var container = (ResponseContainer)context.Object!;

        var ts = new TripleStore();
        ts.Add(container.Graph);

        if (container.Frame is JToken frame)
        {
            var jsonLdNode = new JsonLdWriter().SerializeStore(ts);
            using var writer = new JsonTextWriter(streamWriter);
            await JsonLdProcessor
                .Frame(jsonLdNode, frame, new JsonLdProcessorOptions())
                .WriteToAsync(writer);
        }
        else
        {
            new JsonLdWriter().Save(ts, streamWriter);
        }
    }

    protected override bool CanWriteType(Type? type) => type!.IsAssignableFrom(typeof(ResponseContainer));
}
