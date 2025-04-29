using Microsoft.AspNetCore.Mvc;
using Sim.Api.Validators;

namespace Sim.Api;

internal class OutOfTapeException(string tape) : Exception(tape);

public class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (Exception exception)
        {
            CancellationToken cancellationToken = httpContext.RequestAborted;
            ProblemDetails problemDetails;
            var extensions =
                new Dictionary<string, object> { };
            switch (exception)
            {
                case OutOfTapeException:
                    problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Out of tape",
                        Detail = exception.Message,
                        Extensions = extensions
                    };
                    httpContext.Response.StatusCode = problemDetails.Status.Value;
                    break;
                case ValidationException:
                    problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Bad request",
                        Detail = exception.Message,
                        Extensions = extensions
                    };
                    httpContext.Response.StatusCode = problemDetails.Status.Value;
                    break;

                default:
                    var message = exception.Message;
                    message += System.Environment.NewLine + exception.StackTrace;
                    problemDetails = new ProblemDetails
                    {
                        Status = StatusCodes.Status500InternalServerError,
                        Title = "Server error",
                        Detail = message,
                        Extensions = extensions
                    };
                    break;
            }

            await httpContext.Response
                .WriteAsJsonAsync(problemDetails, cancellationToken);
        }
    }
}
