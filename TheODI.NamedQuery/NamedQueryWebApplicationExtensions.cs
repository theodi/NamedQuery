using Microsoft.Extensions.Options;
using TheODI.NamedQuery;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods for adding named SPARQL queries to an application's request pipeline.</summary>
public static class NamedQueryWebApplicationExtensions
{
    /// <summary>Turns on named SPARQL queries in the application's request pipeline, so that the endpoints described in the manifest are served when the application runs.</summary>
    /// <param name="app">The application returned by <c>builder.Build()</c>.</param>
    /// <returns>The same application, for chaining.</returns>
    /// <remarks>
    /// <para>Call it once in <c>Program.cs</c>, after <c>builder.Build()</c> and before <c>app.Run()</c>. The services it needs must already be registered with <see cref="Microsoft.Extensions.DependencyInjection.NamedQueryServiceCollectionExtensions.AddNamedQuery"/>.</para>
    /// <para>It adds the CORS middleware at the point where it is called, so middleware added before it (for example exception handling, HTTPS redirection or authentication) runs first for named query requests.</para>
    /// <para>It maps the REST API (OpenAPI description and endpoints) unless <see cref="NamedQueryOptions.Rest"/> is <see langword="false"/>, and the MCP server at <see cref="NamedQueryOptions.McpPath"/> unless <see cref="NamedQueryOptions.Mcp"/> is <see langword="false"/>.</para>
    /// <para>It maps the application's MVC controllers, so your own controllers are mapped too and you don't need to call <c>MapControllers</c> yourself. Your own endpoints can be mapped before or after it, as long as their routes don't collide with endpoint paths in the manifest.</para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    /// using Microsoft.Extensions.FileProviders;
    ///
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddNamedQuery(options => options.Definitions = new ManifestEmbeddedFileProvider(typeof(Program).Assembly));
    ///
    /// var app = builder.Build();
    /// app.UseHttpsRedirection();
    /// app.UseNamedQuery();
    /// app.MapGet("/health", () => "OK");
    ///
    /// app.Run();
    /// </code>
    /// </example>
    public static WebApplication UseNamedQuery(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<NamedQueryOptions>>().Value;

        app.UseCors();
        app.MapControllers();

        if (options.Mcp)
        {
            app.MapMcp(options.McpPath);
        }

        return app;
    }
}
