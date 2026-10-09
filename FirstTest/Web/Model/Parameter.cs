using VDS.RDF.Parsing;

namespace Web.Model;

public partial class Parameter
{
    public Uri DatatypeInternal => Datatype ?? new Uri(XmlSpecsHelper.XmlSchemaDataTypeString);
}
