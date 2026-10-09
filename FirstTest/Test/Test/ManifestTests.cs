using Web;
using Web.Model;

namespace Test;

[TestClass]
public sealed class ManifestTests
{
    private static readonly Manifest manifest = Resources.Manifest;

    [TestMethod]
    public void EnumeratesEndpoints()
    {
        manifest.Endpoints.Select(endpoint => endpoint.Path).Should().BeEquivalentTo("endpoint1", "endpoint2", "endpoint3/something", "endpoint4", "endpoint5", "endpoint6");
    }

    [TestMethod]
    public void ReadsParameters()
    {
        manifest["endpoint6"]!.Parameters.Should().ContainSingle().Which.Should().Match<Parameter>(parameter =>
            parameter.Name == "value" &&
            parameter.DatatypeInternal == new Uri("http://www.w3.org/2001/XMLSchema#string"));
    }

    [TestMethod]
    public void EndpointWithoutParametersHasNone()
    {
        manifest["endpoint1"]!.Parameters.Should().BeEmpty();
    }

    [TestMethod]
    public void FindsEndpointByPath()
    {
        manifest["endpoint1"].Should().NotBeNull().And.Subject.As<Endpoint>().Path.Should().Be("endpoint1");
    }

    [TestMethod]
    public void UnknownPathIsNull()
    {
        manifest["unknown"].Should().BeNull();
    }

    [TestMethod]
    public void ResolvesSparqlResourceName()
    {
        manifest["endpoint1"]!.SparqlResourceName.Should().Be("endpoint1.sparql");
    }

    [TestMethod]
    public void ReadsQuery()
    {
        manifest["endpoint2"]!.Query.Should().StartWith("# LocalDevelopmentEndpointDefinitions/endpoint2.sparql").And.EndWith("ASK {}");
    }
}
