namespace Sim.Api.Controllers;

public class SimulationsDataEntryModel
{
    public string Route { get; set; }
    public string Method { get; set; }
    public string Persistence { get; set; }
    public int ResponseStatusCode { get; set; }
    public string? BodyRegex { get; set; }
    public string? ResponseBody { get; set; }
    public string? ResponseContentType { get; set; }
    public string? Tape { get; set; }
    public byte[]? File { get; set; }
    public string? FileName { get; set; }

    public static SimulatorDataEntry ToSimulatorDataEntry(
        SimulationsDataEntryModel model
    )
    {
        var persistence = (SimulatorDataEntryPersistence)
            Enum.Parse(typeof(SimulatorDataEntryPersistence), model.Persistence);
        var queryCollection = new Dictionary<string, string>();

        var route = model.Route.Trim();
        if (!route.StartsWith('/'))
            route = $"/{route}";

        if (route.IndexOf('?') > 0)
        {
            // Remove the query string from the route
            route = route.Substring(0, route.IndexOf('?'));
            // parse query string
            var queryString = model.Route.Contains('?')
                ? model.Route.Substring(model.Route.IndexOf('?') + 1)
                : string.Empty;
            var queries = queryString.Split('&');
            foreach (var query in queries)
            {
                var keyValue = query.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim();
                    var value = keyValue[1].Trim();
                    queryCollection.Add(key, value);
                }
            }
        }

        return new SimulatorDataEntry(
            DateTime.UtcNow,
            route,
            model.BodyRegex,
            queryCollection.Select(q => $"{q.Key}={q.Value}"),
            new HttpMethod(model.Method),
            model.ResponseStatusCode,
            persistence,
            model.ResponseBody,
            model.ResponseContentType,
            model.Tape,
            model.File,
            model.FileName
        );
    }
}
