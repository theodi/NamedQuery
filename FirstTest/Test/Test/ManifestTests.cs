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
        manifest.Endpoints.Select(endpoint => endpoint.Path).Should().BeEquivalentTo("endpoint1", "endpoint2", "endpoint3/something", "endpoint4", "endpoint5");
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
