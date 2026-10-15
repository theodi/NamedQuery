using Microsoft.OpenApi;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Expressions;

namespace TheODI.NamedQuery.Model;

internal partial class Parameter
{
    public Uri Datatype => DatatypeInternal ?? new Uri(XmlSpecsHelper.XmlSchemaDataTypeString);

    public JsonSchemaType JsonSchemaType => NumericTypesHelper.GetNumericTypeFromDataTypeUri(Datatype) switch
    {
        SparqlNumericType.Integer =>
            JsonSchemaType.Integer,

        SparqlNumericType.Decimal
        or SparqlNumericType.Float
        or SparqlNumericType.Double =>
            JsonSchemaType.Number,

        _ when Datatype.AbsoluteUri == XmlSpecsHelper.XmlSchemaDataTypeBoolean =>
            JsonSchemaType.Boolean,

        _ =>
            JsonSchemaType.String,
    };
}
