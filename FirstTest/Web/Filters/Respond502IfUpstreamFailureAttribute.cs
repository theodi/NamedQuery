using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using VDS.RDF.Query;

namespace Web.Filters;

internal class Respond502IfUpstreamFailureAttribute : TypeFilterAttribute<Respond502IfUpstreamFailureAttribute.Filter>
{
    internal class Filter(ProblemDetailsFactory problemDetailsFactory) : IExceptionFilter
    {
        void IExceptionFilter.OnException(ExceptionContext exception)
        {
            if (exception.Exception is not (RdfQueryException or HttpRequestException))
            {
                return;
            }

            var problem = problemDetailsFactory.CreateProblemDetails(
                exception.HttpContext,
                StatusCodes.Status502BadGateway,
                "SPARQL endpoint failed",
                detail: string.Join(" ---> ", exception.Exception.Messages()));

            exception.Result = new JsonResult(problem)
            {
                StatusCode = problem.Status,
                ContentType = MediaTypeNames.Application.ProblemJson
            };
        }
    }
}
