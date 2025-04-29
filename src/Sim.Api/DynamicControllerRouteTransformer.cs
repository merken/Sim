using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Sim.Api;

public class DynamicControllerRouteTransformer(
    ISimulationsDataStore simulationsDataStore,
    ILogger<DynamicControllerRouteTransformer> logger)
    : DynamicRouteValueTransformer
{
    private static readonly string[] KnownControllerRoutes =
        typeof(DynamicControllerRouteTransformer).Assembly
            .GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Controller)))
            .Select(t =>
                (
                    t.GetCustomAttributes(typeof(RouteAttribute), false).FirstOrDefault()
                        as RouteAttribute
                ).Template
            )
            .ToArray()
            .Concat(["api/simulations/purge"])
            .ToArray();

    private bool IsSwaggerRoute(string route)
    {
        return route.StartsWith("swagger", StringComparison.OrdinalIgnoreCase);
    }

    public override async ValueTask<RouteValueDictionary> TransformAsync(
        HttpContext httpContext,
        RouteValueDictionary values
    )
    {
        if (values["route"] == null)
            throw new NotSupportedException($"No route found");
        var route = values["route"].ToString();
        if (KnownControllerRoutes.Any(r => r == route))
            return values;

        if (IsSwaggerRoute(route))
        {
            logger.LogInformation("Swagger route detected: {route}", route);
            return values;
        }

        var tape = httpContext.Request.Headers["X-Simulator-Tape"];
        if (!String.IsNullOrEmpty(tape))
            logger.LogInformation("Tape detected: {tape}", tape);

        var body = await PeekBody(httpContext.Request);
        var entryResult = await simulationsDataStore.FindEntry(
            route,
            httpContext.Request.Method,
            body,
            httpContext.Request.Query,
            tape,
            httpContext.RequestAborted
        );

        if (entryResult.OutOfTape)
        {
            values = new RouteValueDictionary
            {
                ["controller"] = "Dynamic",
                ["action"] = "OutOfTape",
                ["tape"] = entryResult.Tape
            };
            return values;
        }

        if (!entryResult.HasResult)
        {
            logger.LogInformation("No entry found for {route} {method} {tape} {query}", route,
                httpContext.Request.Method, tape, httpContext.Request.Query);
            return values;
        }

        values = new RouteValueDictionary
        {
            ["controller"] = "Dynamic",
            ["action"] = "GetEntry",
            ["id"] = entryResult.Id
        };

        logger.LogInformation("Matching entry found for {route} {method} {tape} {query} {id}", route,
            httpContext.Request.Method, tape, httpContext.Request.Query, entryResult.Id);

        return values;
    }

    private static async Task<string> PeekBody(HttpRequest request)
    {
        try
        {
            request.EnableBuffering();
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            if (buffer.Length == 0)
                return string.Empty;

            var encoding = new UTF8Encoding();
            await request.Body.ReadExactlyAsync(buffer);
            return encoding.GetString(buffer);
        }
        finally
        {
            request.Body.Position = 0;
        }
    }
}
