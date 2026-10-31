using Swashbuckle.AspNetCore.SwaggerUI;
using TheODI.NamedQuery;
using TheODI.NamedQuery.Constraints;
using TheODI.NamedQuery.Controllers;
using TheODI.NamedQuery.Formatters;
using TheODI.NamedQuery.Model;
using static VDS.RDF.MimeTypesHelper;
using DotNetRDF = VDS.RDF.Query;

namespace Microsoft.Extensions.DependencyInjection;

public static class NamedQueryServiceCollectionExtensions
{
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
