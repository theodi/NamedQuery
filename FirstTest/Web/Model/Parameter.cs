using VDS.RDF.Parsing;

namespace Web.Model;

public partial class Parameter
{
    public Uri Datatype => DatatypeInternal ?? new Uri(XmlSpecsHelper.XmlSchemaDataTypeString);
}
