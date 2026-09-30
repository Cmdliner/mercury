using Mercury.Payments;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Mercury.Api;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception ({TraceId}) on {Method} {Path}",
            httpContext.TraceIdentifier, httpContext.Request.Method, httpContext.Request.Path);

        var (statusCode, title, detail) = exception switch
        {
            PaymentProviderException ex => (StatusCodes.Status502BadGateway, "Payment provider rejected the request", ex.ProviderMessage),
            ArgumentException ex => (StatusCodes.Status400BadRequest, "Invalid request", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "Something went wrong",
                "An unexpected error occurred. Check the server logs for the trace id below.")
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }, cancellationToken);

        return true;
    }
}
