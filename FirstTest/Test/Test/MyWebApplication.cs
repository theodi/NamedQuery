using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Web;

namespace Test;

internal class MyWebApplication
{
    private readonly WebApplication app;
    private HttpClient? client;

    internal MyWebApplication()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["NamedQuery:SparqlEndpoint"] = "http://sparql.test/";
        builder.Services.AddNamedQuery(options => options.Definitions = new ManifestEmbeddedFileProvider(typeof(MyWebApplication).Assembly));
        builder.Services.ConfigureHttpClientDefaults(client => client.ConfigurePrimaryHttpMessageHandler(() => new SparqlStub()));

        app = builder.Build();
        app.UseNamedQuery();
        app.Start();
    }

    internal IServiceProvider Services => app.Services;

    // Follows redirects like WebApplicationFactory.CreateClient
    internal HttpClient Client => client ??= new(new RedirectHandler { InnerHandler = app.GetTestServer().CreateHandler() }) { BaseAddress = app.GetTestServer().BaseAddress };
}
