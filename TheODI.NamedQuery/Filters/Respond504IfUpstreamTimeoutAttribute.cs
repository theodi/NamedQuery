using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace TheODI.NamedQuery.Filters;

internal class Respond504IfUpstreamTimeoutAttribute : TypeFilterAttribute<Respond504IfUpstreamTimeoutAttribute.Filter>
{
    internal class Filter(ProblemDetailsFactory problemDetailsFactory) : IExceptionFilter
    {
        void IExceptionFilter.OnException(ExceptionContext exception)
        {
            if (exception.Exception is not TaskCanceledException { InnerException: TimeoutException })
            {
                return;
            }

            var problem = problemDetailsFactory.CreateProblemDetails(
                exception.HttpContext,
                StatusCodes.Status504GatewayTimeout,
                "SPARQL endpoint timed out",
                detail: string.Join(" ---> ", exception.Exception.Messages()));

            exception.Result = new JsonResult(problem)
            {
                StatusCode = problem.Status,
                ContentType = MediaTypeNames.Application.ProblemJson
            };
        }
    }
}
