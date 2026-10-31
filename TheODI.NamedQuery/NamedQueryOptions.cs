using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.FileProviders;

namespace TheODI.NamedQuery;

/// <summary>Options for serving named SPARQL queries: which SPARQL endpoint to query and where the endpoint definitions come from.</summary>
/// <remarks>
/// <para>The options are bound from the <c>NamedQuery</c> configuration section (<see cref="SectionName"/>), then the delegate passed to <see cref="Microsoft.Extensions.DependencyInjection.NamedQueryServiceCollectionExtensions.AddNamedQuery"/> is applied, so code takes precedence over configuration.</para>
/// <para>Both <see cref="SparqlEndpoint"/> and <see cref="Definitions"/> are required. They are validated when the host starts, and startup fails with an <see cref="Microsoft.Extensions.Options.OptionsValidationException"/> if either is missing.</para>
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
public class NamedQueryOptions
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
    /// <description><c>:name</c> and <c>:description</c>: shown on the home page, in OpenAPI and as the MCP tool title and description.</description>
    /// </item>
    /// <item>
    /// <description><c>:frame</c> (optional): the relative path of a JSON-LD frame applied to graph results served as JSON-LD.</description>
    /// </item>
    /// <item>
    /// <description><c>:parameter</c> (zero or more): each with a <c>:name</c>, <c>:description</c> and <c>:example</c>, and an optional <c>:datatype</c> (default <c>xsd:string</c>).</description>
    /// </item>
    /// </list>
    /// <para>A parameter is supplied as a query string parameter of the same name, exactly once, and is bound to the SPARQL variable of the same name as a literal of its datatype. Every parameter is required. <c>format</c> is reserved for choosing the response format.</para>
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
}
