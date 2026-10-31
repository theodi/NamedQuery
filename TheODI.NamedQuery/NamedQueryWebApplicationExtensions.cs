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
    /// <para>It adds the CORS and Swagger UI middleware at the point where it is called, so middleware added before it (for example exception handling, HTTPS redirection or authentication) runs first for named query requests.</para>
    /// <para>It maps the application's MVC controllers and Razor Pages, so your own controllers and pages are mapped too and you don't need to call <c>MapControllers</c> or <c>MapRazorPages</c> yourself. Your own endpoints can be mapped before or after it, as long as their routes don't collide with endpoint paths in the manifest.</para>
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
        app.UseSwaggerUI();
        app.UseCors();
        app.MapControllers();
        app.MapRazorPages();
        app.MapMcp(Mcp.Route);

        return app;
    }
}
