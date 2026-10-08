using Newtonsoft.Json.Linq;
using VDS.RDF;

namespace Web.Model;

internal class Response
{
    internal required JToken? Frame { get; set; }

    internal required IGraph Graph { get; set; }
}
