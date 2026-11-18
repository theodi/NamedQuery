using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Routing.Patterns;
using System.ComponentModel.DataAnnotations;

namespace TheODI.NamedQuery;

/// <summary>Options for serving named SPARQL queries: which SPARQL endpoint to query, where the endpoint definitions come from, and whether they are served as a REST API, an MCP server or both.</summary>
/// <remarks>
/// <para>The options are bound from the <c>NamedQuery</c> configuration section (<see cref="SectionName"/>), then the delegate passed to <see cref="Microsoft.Extensions.DependencyInjection.NamedQueryServiceCollectionExtensions.AddNamedQuery"/> is applied, so code takes precedence over configuration.</para>
/// <para>Both <see cref="SparqlEndpoint"/> and <see cref="Definitions"/> are required, at least one of <see cref="Rest"/> and <see cref="Mcp"/> must be <see langword="true"/>, and <see cref="McpPath"/> must be a valid path. They are validated when the host starts, and startup fails with an <see cref="Microsoft.Extensions.Options.OptionsValidationException"/> otherwise.</para>
/// </remarks>
/// <example>
/// <para>The SPARQL endpoint usually comes from configuration, for example <c>appsettings.json</c>:</para>
/// <code language="json">
/// {
///   "NamedQuery": {
///     "SparqlEndpoint": "https://example.com/sparql"
///   }
/// }
/// </code>
/// <para>or the environment variable <c>NamedQuery__SparqlEndpoint=https://example.com/sparql</c>, while the definitions are set in code:</para>
/// <code language="csharp">
/// builder.Services.AddNamedQuery(options => options.Definitions = new ManifestEmbeddedFileProvider(typeof(Program).Assembly));
/// </code>
/// </example>
public class NamedQueryOptions : IValidatableObject
{
    /// <summary>The name of the configuration section the options are bound from: <c>NamedQuery</c>.</summary>
    public const string SectionName = "NamedQuery";

    /// <summary>Gets or sets the absolute URI of the SPARQL 1.1 query endpoint that every named query is executed against.</summary>
    /// <remarks>
    /// <para>Typically set from configuration as <c>NamedQuery:SparqlEndpoint</c>.</para>
    /// <para>Upstream failures are returned to clients as <c>502 Bad Gateway</c> and timeouts as <c>504 Gateway Timeout</c>, both with problem details.</para>
    /// </remarks>
    [Required]
    public required Uri SparqlEndpoint { get; set; }

    /// <summary>Gets or sets the file provider that supplies the endpoint definitions: a <c>manifest.ttl</c> at its root and the files the manifest refers to.</summary>
    /// <remarks>
    /// <para>The manifest is Turtle using the vocabulary <c>http://example.org/namedquery/</c>. It has exactly one manifest node with a <c>:name</c> and one or more <c>:endpoint</c> values.</para>
    /// <para>Each endpoint has:</para>
    /// <list type="bullet">
    /// <item>
    /// <description><c>:path</c>: the unique URL path it is served at, without a leading slash; it may contain slashes.</description>
    /// </item>
    /// <item>
    /// <description><c>:sparql</c>: the relative path of its SPARQL query file, as an <c>xsd:anyURI</c> starting with <c>./</c>.</description>
    /// </item>
    /// <item>
    /// <description><c>:name</c> and <c>:description</c>: shown in OpenAPI and as the MCP tool title and description.</description>
    /// </item>
    /// <item>
    /// <description><c>:frame</c> (optional): the relative path of a JSON-LD frame applied to graph results served as JSON-LD.</description>
    /// </item>
    /// <item>
    /// <description><c>:parameter</c> (zero or more): each with a <c>:name</c>, <c>:description</c> and <c>:example</c>, and an optional <c>:datatype</c> (default <c>xsd:string</c>).</description>
    /// </item>
    /// </list>
    /// <para>A parameter is supplied as a query string parameter of the same name, exactly once, and is bound to the SPARQL variable of the same name as a literal of its datatype. Every parameter is required.</para>
    /// <para>Use a <see cref="ManifestEmbeddedFileProvider"/> to embed the definitions in the host assembly, or a <see cref="PhysicalFileProvider"/> to read them from a folder at runtime.</para>
    /// </remarks>
    /// <example>
    /// <para><c>manifest.ttl</c>:</para>
    /// <code language="turtle">
    /// <![CDATA[
    /// PREFIX : <http://example.org/namedquery/>
    /// PREFIX xsd: <http://www.w3.org/2001/XMLSchema#>
    ///
    /// [
    ///     :name "Example endpoints" ;
    ///     :endpoint [
    ///         :path "triples-by-object" ;
    ///         :sparql "./triples-by-object.sparql"^^xsd:anyURI ;
    ///         :name "Triples by object" ;
    ///         :description "Selects triples whose object is the supplied value." ;
    ///         :parameter [
    ///             :name "value" ;
    ///             :description "Literal object to match." ;
    ///             :example "example" ;
    ///         ] ;
    ///     ] ;
    /// ] .
    /// ]]>
    /// </code>
    /// <para><c>triples-by-object.sparql</c>, where <c>$value</c> is replaced by the <c>value</c> parameter:</para>
    /// <code language="sparql">
    /// SELECT * {
    ///     BIND($value AS ?o)
    ///     ?s ?p ?o .
    /// }
    /// </code>
    /// <para>Embedding a definitions folder in the host project (requires the <c>Microsoft.Extensions.FileProviders.Embedded</c> package):</para>
    /// <code language="xml">
    /// <![CDATA[
    /// <PropertyGroup>
    ///   <GenerateEmbeddedFilesManifest>true</GenerateEmbeddedFilesManifest>
    /// </PropertyGroup>
    /// <ItemGroup>
    ///   <EmbeddedResource Include="EndpointDefinitions\**\*.*" TargetPath="%(RecursiveDir)%(Filename)%(Extension)" />
    /// </ItemGroup>
    /// ]]>
    /// </code>
    /// <code language="csharp">
    /// options.Definitions = new ManifestEmbeddedFileProvider(typeof(Program).Assembly);
    /// </code>
    /// <para>Reading the definitions from a folder instead:</para>
    /// <code language="csharp">
    /// options.Definitions = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "EndpointDefinitions"));
    /// </code>
    /// </example>
    [Required]
    public required IFileProvider Definitions { get; set; }

    /// <summary>Gets or sets a value indicating whether the endpoints are served as a REST API, with the OpenAPI description at <c>/</c> and each endpoint at its path. Defaults to <see langword="true"/>.</summary>
    /// <remarks>
    /// <para>Typically set from configuration as <c>NamedQuery:Rest</c>, for example with the environment variable <c>NamedQuery__Rest=false</c> to serve only the MCP server.</para>
    /// <para>Turning it off removes only this library's controllers. The host application's own controllers are still mapped.</para>
    /// </remarks>
    public bool Rest { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the endpoints are served as tools on the MCP server at <see cref="McpPath"/>. Defaults to <see langword="true"/>.</summary>
    /// <remarks>
    /// <para>Typically set from configuration as <c>NamedQuery:Mcp</c>, for example with the environment variable <c>NamedQuery__Mcp=false</c> to serve only the REST API.</para>
    /// <para>To leave out individual endpoints instead, set <c>:mcp false</c> on them in the manifest.</para>
    /// </remarks>
    public bool Mcp { get; set; } = true;

    /// <summary>Gets or sets the URL path of the MCP server. Defaults to <c>/mcp</c>.</summary>
    /// <remarks>
    /// <para>Typically set from configuration as <c>NamedQuery:McpPath</c>. It must start with <c>/</c> and must not contain route parameters.</para>
    /// <para>It can be <c>/</c> only when <see cref="Rest"/> is <see langword="false"/>, because the REST API serves its OpenAPI description there. When the REST API is on, it must not be the path of an endpoint in the manifest; build-time validation reports such a collision as <c>NQ1006</c>.</para>
    /// </remarks>
    public string McpPath { get; set; } = "/mcp";

    /// <inheritdoc/>
    IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
    {
        if (!Rest && !Mcp)
        {
            yield return new ValidationResult($"At least one of {nameof(Rest)} and {nameof(Mcp)} must be true.", [nameof(Rest), nameof(Mcp)]);
        }

        if (!McpPath.StartsWith('/'))
        {
            yield return new ValidationResult($"{nameof(McpPath)} must start with '/'.", [nameof(McpPath)]);
        }
        else if (!IsLiteralRoute(McpPath))
        {
            yield return new ValidationResult($"{nameof(McpPath)} must be a path without route parameters.", [nameof(McpPath)]);
        }
        else if (Rest && Mcp && McpPath.Trim('/').Length == 0)
        {
            yield return new ValidationResult($"{nameof(McpPath)} can be '/' only when {nameof(Rest)} is false, because the OpenAPI description is served there.", [nameof(McpPath), nameof(Rest)]);
        }
    }

    private static bool IsLiteralRoute(string path)
    {
        try
        {
            return RoutePatternFactory.Parse(path).Parameters.Count == 0;
        }
        catch (RoutePatternException)
        {
            return false;
        }
    }
}
