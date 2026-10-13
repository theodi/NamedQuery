using Swashbuckle.AspNetCore.SwaggerUI;
using Web.Constraints;
using Web.Controllers;
using Web.Formatters;
using Web.Middleware;
using Web.Model;
using static VDS.RDF.MimeTypesHelper;
using DotNetRDF = VDS.RDF.Query;

namespace Web;

public static class NamedQueryExtensions
{
    public static IServiceCollection AddNamedQuery(this IServiceCollection services, Action<Options> configure)
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

            options.OutputFormatters.Insert(0, new OpenApiYamlFormatter());
            options.OutputFormatters.Insert(0, new OpenApiJsonFormatter());
            options.OutputFormatters.Insert(0, new SparqlFormatter());
            options.OutputFormatters.Insert(0, new GraphFormatter());
            options.OutputFormatters.Insert(0, new JsonLdFormatter());
        });
        services.AddRazorPages();
        services.Configure<SwaggerUIOptions>(options => options.SwaggerEndpoint(OpenApiController.Route, OpenApiController.Name));
        services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
        services.AddOptions<Options>().BindConfiguration(Options.SectionName).Configure(configure).ValidateDataAnnotations().ValidateOnStart();
        services.AddHttpClient<DotNetRDF.ISparqlQueryClient, SparqlQueryClient>();
        services.AddScoped<Context>();
        services.AddSingleton<Resources>();
        services.AddRouting(options => options.SetParameterPolicy<ManifestEndpointConstraint>(ManifestEndpointConstraint.Name));
        services.AddTransient<PopulateEndpointInContextMiddleware>();

        return services;
    }

    public static WebApplication UseNamedQuery(this WebApplication app)
    {
        app.UseMiddleware<PopulateEndpointInContextMiddleware>();
        app.UseSwaggerUI();
        app.UseRouting();
        app.UseCors();
        app.MapControllers();
        app.MapRazorPages();

        return app;
    }
}
