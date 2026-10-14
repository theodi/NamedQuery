namespace Web.Filters;

internal static class ExceptionExtensions
{
    internal static IEnumerable<string> Messages(this Exception? exception)
    {
        for (; exception is not null; exception = exception.InnerException)
        {
            yield return exception.Message;
        }
    }
}
