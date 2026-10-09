using Web.Model;

namespace Web.Constraints;

internal class ManifestEndpointConstraint : IRouteConstraint
{
    bool IRouteConstraint.Match(HttpContext? http, IRouter? _, string routeKey, RouteValueDictionary routeValues, RouteDirection routeDirection)
    {
        if (routeDirection != RouteDirection.IncomingRequest)
        {
            throw new NotImplementedException();
        }

        var context = http!.RequestServices.GetRequiredService<Context>();

        if (context.Endpoint is null)
        {
            return false;
        }

        return context.Endpoint.Path == routeValues[routeKey] as string;
    }
}
