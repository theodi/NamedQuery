using Swashbuckle.AspNetCore.SwaggerUI;
using Web;
using Web.Constraints;
using Web.Controllers;
using Web.Formatters;
using Web.Middleware;
using Web.Model;
using static VDS.RDF.MimeTypesHelper;
using DotNetRDF = VDS.RDF.Query;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(options =>
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
builder.Services.AddRazorPages();
builder.Services.Configure<SwaggerUIOptions>(options => options.SwaggerEndpoint(OpenApiController.Route, OpenApiController.Name));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
builder.Services.AddOptions<Options>().BindConfiguration(Options.SectionName).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddHttpClient<DotNetRDF.ISparqlQueryClient, SparqlQueryClient>();
builder.Services.AddScoped<Context>();
builder.Services.AddSingleton<Resources>();
builder.Services.AddRouting(options => options.SetParameterPolicy<ManifestEndpointConstraint>(ManifestEndpointConstraint.Name));
builder.Services.AddTransient<PopulateEndpointInContextMiddleware>();

var app = builder.Build();
app.UseMiddleware<PopulateEndpointInContextMiddleware>();
app.UseSwaggerUI();
app.UseRouting();
app.UseCors();
app.MapControllers();
app.MapRazorPages();

app.Run();
