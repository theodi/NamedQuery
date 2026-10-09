using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Web.Model;

namespace Web.Filters;

internal class Respond404IfMissingEndpointAttribute : TypeFilterAttribute<Respond404IfMissingEndpointAttribute.Filter>
{
    internal class Filter(Context context) : IResourceFilter
    {
        void IResourceFilter.OnResourceExecuting(ResourceExecutingContext resource)
        {
            if (context.Endpoint is null)
            {
                resource.Result = new NotFoundResult();
            }
        }

        void IResourceFilter.OnResourceExecuted(ResourceExecutedContext _) { }
    }
}
