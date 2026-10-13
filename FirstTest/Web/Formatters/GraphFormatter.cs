using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text;
using VDS.RDF;
using Web.Model;

namespace Web.Formatters;

internal class GraphFormatter : TextOutputFormatter
{
    public GraphFormatter()
    {
        foreach (var mime in MediaTypes)
        {
            SupportedMediaTypes.Add(mime);
        }

        SupportedEncodings.Add(Encoding.UTF8);
    }

    internal static IEnumerable<string> MediaTypes => MimeTypesHelper.Definitions
        .Where(static definition => definition.CanWriteRdfDatasets || definition.CanWriteRdf)
        .Select(static definition => definition.CanonicalMimeType)
        .Distinct();

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        using var streamWriter = new StreamWriter(context.HttpContext.Response.Body, selectedEncoding);

        var graph = ((Response)context.Object!).Graph;

        var datasetWriter = MimeTypesHelper
            .GetDefinitions(context.ContentType.ToString())
            .Where(static definition => definition.CanWriteRdfDatasets)
            .Select(static definition => definition.GetRdfDatasetWriter())
            .FirstOrDefault();

        if (datasetWriter is not null)
        {
            var ts = new TripleStore();
            ts.Add(graph);

            datasetWriter.Save(ts, streamWriter);
        }
        else
        {
            var rdfWriter = MimeTypesHelper
                .GetDefinitions([context.ContentType.ToString(), .. MimeTypesHelper.Turtle])
                .First(static definition => definition.CanWriteRdf)
                .GetRdfWriter();

            rdfWriter.Save(graph, streamWriter);
        }

        return Task.CompletedTask;
    }

    protected override bool CanWriteType(Type? type) => typeof(Response).IsAssignableFrom(type);
}
