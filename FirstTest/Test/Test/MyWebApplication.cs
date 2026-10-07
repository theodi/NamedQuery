using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Test;

internal class MyWebApplication : WebApplicationFactory<Program>
{
    private HttpClient? client;

    internal HttpClient Client => client ??= CreateClient();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Options:SparqlEndpoint", "http://sparql.test/");
        builder.ConfigureServices(services => services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => new SparqlStub())));
    }
}
