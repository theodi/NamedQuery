namespace Web.Constraints;

internal class ManifestEndpointConstraint(Resources resources) : IRouteConstraint
{
    internal const string Name = "ManifestEndpoint";

    bool IRouteConstraint.Match(HttpContext? _, IRouter? __, string routeKey, RouteValueDictionary routeValues, RouteDirection ___) =>
        routeValues[routeKey] is string path && resources.Manifest[path] is not null;
}
