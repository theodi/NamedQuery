using Swashbuckle.AspNetCore.SwaggerUI;
using TheODI.NamedQuery;
using TheODI.NamedQuery.Constraints;
using TheODI.NamedQuery.Controllers;
using TheODI.NamedQuery.Formatters;
using TheODI.NamedQuery.Model;
using static VDS.RDF.MimeTypesHelper;
using DotNetRDF = VDS.RDF.Query;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for adding named SPARQL queries to an application's services.</summary>
public static class NamedQueryServiceCollectionExtensions
{
    /// <summary>Adds everything an application needs to serve named SPARQL queries, and tells it which SPARQL endpoint to query and where to find the endpoint definitions.</summary>
    /// <param name="services">The application's services, usually <c>builder.Services</c>.</param>
    /// <param name="configure">Sets up <see cref="NamedQueryOptions"/>, typically by setting <see cref="NamedQueryOptions.Definitions"/>. It runs after the options are read from the <c>NamedQuery</c> configuration section, so values set here win.</param>
    /// <returns>The same services, for chaining.</returns>
    /// <remarks>
    /// <para>Call it once in <c>Program.cs</c>, on <c>builder.Services</c> before <c>builder.Build()</c>, then call <see cref="Microsoft.AspNetCore.Builder.NamedQueryWebApplicationExtensions.UseNamedQuery"/> on the built application.</para>
    /// <para>The SPARQL endpoint usually comes from configuration (<c>appsettings.json</c>, environment variables or command-line arguments) as <c>NamedQuery:SparqlEndpoint</c>. If it or the definitions are missing, the application fails to start; see <see cref="NamedQueryOptions"/>.</para>
    /// <para>It also registers MVC controllers, Razor Pages, CORS and the MCP server for you, so you don't need to call <c>AddControllers</c>, <c>AddRazorPages</c>, <c>AddCors</c> or <c>AddMcpServer</c> yourself. Its settings apply to the whole application: your own controllers get the same output formatters, <c>format</c> query string mappings, <c>RespectBrowserAcceptHeader</c> and <c>ReturnHttpNotAcceptable</c>, and the default CORS policy allows any origin, method and header.</para>
    /// </remarks>
    /// <example>
    /// <para>A complete <c>Program.cs</c>, with the definitions embedded in the application (see <see cref="NamedQueryOptions.Definitions"/>) and the SPARQL endpoint in configuration:</para>
    /// <code language="csharp">
    /// using Microsoft.Extensions.FileProviders;
    ///
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddNamedQuery(options => options.Definitions = new ManifestEmbeddedFileProvider(typeof(Program).Assembly));
    ///
    /// var app = builder.Build();
    /// app.UseNamedQuery();
    ///
    /// app.Run();
    /// </code>
    /// </example>
    public static IServiceCollection AddNamedQuery(this IServiceCollection services, Action<NamedQueryOptions> configure)
    {
        services.AddControllers(options =>
        {
            options.FormatterMappings.SetMediaTypeMappingForFormat(DefaultCsvExtension, Csv[0]);
            options.FormatterMappings.SetMediaTypeMappingForFormat(DefaultJsonLdExtension, JsonLd[0]);
            options.FormatterMappings.SetMediaTypeMappingForFormat(DefaultTurtleExtension, Turtle[0]);
            options.FormatterMappings.SetMediaTypeMappingForFormat(DefaultRdfXmlExtension, RdfXml[0]);
            options.FormatterMappings.SetMediaTypeMappingForFormat(DefaultNTriplesExtension, NTriples[0]);
            options.FormatterMappings.SetMediaTypeMappingForFormat(DefaultHtmlExtension, Html[0]);

            options.RespectBrowserAcceptHeader = true;
            options.ReturnHttpNotAcceptable = true;

            options.OutputFormatters.Insert(0, new JsonLdFormatter());
            options.OutputFormatters.Insert(1, new GraphFormatter());
            options.OutputFormatters.Insert(2, new SparqlFormatter());
            options.OutputFormatters.Insert(3, new OpenApiJsonFormatter());
            options.OutputFormatters.Insert(4, new OpenApiYamlFormatter());
        })
            .AddApplicationPart(typeof(NamedQueryServiceCollectionExtensions).Assembly)
            .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new InternalControllerFeatureProvider()));
        services.AddRazorPages();
        services.Configure<SwaggerUIOptions>(options => options.SwaggerEndpoint(OpenApiController.Route, OpenApiController.Name));
        services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.AddOptions<NamedQueryOptions>().BindConfiguration(NamedQueryOptions.SectionName).Configure(configure).ValidateDataAnnotations().ValidateOnStart();
        services.AddHttpClient<DotNetRDF.ISparqlQueryClient, SparqlQueryClient>();
        services.AddScoped<Context>();
        services.AddSingleton<Resources>();
        services.AddTransient<QueryService>();
        services.AddScoped<Mcp>();
        services.AddRouting(options => options.SetParameterPolicy<ManifestEndpointConstraint>(ManifestEndpointConstraint.Name));
        services.AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true)
            .WithListToolsHandler((request, ct) => request.Services!.GetRequiredService<Mcp>().ListTools(request, ct))
            .WithCallToolHandler((request, ct) => request.Services!.GetRequiredService<Mcp>().CallTool(request, ct));

        return services;
    }
}
