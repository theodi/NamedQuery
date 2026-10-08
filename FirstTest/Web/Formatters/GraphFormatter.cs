using Microsoft.AspNetCore.Mvc.Formatters;
using System.Text;
using VDS.RDF;
using Web.Model;

namespace Web.Formatters;

internal class GraphFormatter : TextOutputFormatter
{
    public GraphFormatter()
    {
        var writers = MimeTypesHelper.Definitions
           .Where(static definition => definition.CanWriteRdfDatasets || definition.CanWriteRdf)
           .Select(static definition => definition.CanonicalMimeType)
           .Distinct();


        foreach (var mime in writers)
        {
            SupportedMediaTypes.Add(mime);
        }

        SupportedEncodings.Add(Encoding.UTF8);
    }

    public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        using var streamWriter = new StreamWriter(context.HttpContext.Response.Body);

        var graph = ((ResponseContainer)context.Object!).Graph;

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

    protected override bool CanWriteType(Type? type) => type!.IsAssignableFrom(typeof(ResponseContainer));
}
