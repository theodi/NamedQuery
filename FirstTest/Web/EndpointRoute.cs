using Microsoft.AspNetCore.Routing.Template;
using System.Diagnostics.CodeAnalysis;
using Web.Controllers;

namespace Web;

internal static class EndpointRoute
{
    internal const string Template = "/{**path:endpoint}";

    private static readonly RouteTemplate routeTemplate = TemplateParser.Parse(Template);
    private static readonly TemplateMatcher templateMatcher = new(routeTemplate, []);
    private static readonly string routeParameterName = routeTemplate.Parameters.Single().Name!;

    internal static bool TryExtractPath(HttpContext httpContext, [NotNullWhen(true)] out string? path)
    {
        var routeValues = new RouteValueDictionary();

        path =
            templateMatcher.TryMatch(httpContext.Request.Path, routeValues)
            ? routeValues[routeParameterName] as string
            : null;
        return path is not null;
    }
}
