using Newtonsoft.Json.Linq;
using VDS.RDF;

namespace TheODI.NamedQuery.Model;

internal class Response
{
    internal required JToken? Frame { get; set; }

    internal required IGraph Graph { get; set; }
}
