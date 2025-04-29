using Sim.Api.Controllers;

namespace Sim.Api.Validators;

public static class RouteValidations
{
    public static List<string> AllowedMethods = new()
    {
        "GET",
        "POST",
        "PUT",
        "DELETE",
        "PATCH",
        "OPTIONS"
    };

    public static List<string> DisallowedMethods = new() { "CONNECT", "TRACE" };

    public static List<string> DisallowedRoutes =
        new()
        {
            "/swagger",
            "/swagger/index.html",
            "/api/simulations",
            "/api/simulations/purge",
            "/api/simulations/file"
        };
}

public class SimulationsDataEntryModelValidator : IValidator<SimulationsDataEntryModel>
{
    public async Task Validate(SimulationsDataEntryModel model, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(model.Route))
            throw new ValidationException($"Route cannot be null or empty");

        if (model.Route.Trim() == "/")
            throw new ValidationException($"Route cannot be '/'");

        if (model.ResponseStatusCode < 100 || model.ResponseStatusCode > 599)
            throw new ValidationException($"ResponseStatusCode is not valid: {model.ResponseStatusCode}");

        if (RouteValidations.DisallowedMethods.Contains(model.Method.ToUpper()))
            throw new ValidationException($"Method '{model.Method}' is not allowed");

        if (RouteValidations.DisallowedRoutes.Contains(model.Route.ToLower()))
            throw new ValidationException($"Route '{model.Route}' is not allowed");

        if (!RouteValidations.AllowedMethods.Contains(model.Method.ToUpper()))
            throw new ValidationException($"Method '{model.Method}' is not part of the allowed methods");
    }
}
