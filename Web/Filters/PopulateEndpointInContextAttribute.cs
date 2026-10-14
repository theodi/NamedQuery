using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Controllers;
using Web.Model;

namespace Web.Filters;

internal class PopulateEndpointInContextAttribute : TypeFilterAttribute<PopulateEndpointInContextAttribute.Filter>
{
    internal class Filter(Context context, Resources resources) : IResourceFilter
    {
        void IResourceFilter.OnResourceExecuting(ResourceExecutingContext resource) =>
            context.Endpoint = resources.Manifest[(string)resource.RouteData.Values[EndpointController.RouteParameter]!];

        void IResourceFilter.OnResourceExecuted(ResourceExecutedContext _) { }
    }
}
