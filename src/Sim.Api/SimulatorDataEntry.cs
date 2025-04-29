namespace Sim.Api;

public class SimulatorDataEntry(
    DateTime added,
    string route,
    string bodyRegex,
    IEnumerable<string> queryParameters,
    HttpMethod method,
    int responseStatusCode,
    SimulatorDataEntryPersistence persistence,
    string? responseBody,
    string? responseContentType,
    string? tape,
    byte[]? file,
    string? fileName
)
{
    public DateTime Added { get; } = added;
    public string Route { get; } = route;
    public HttpMethod Method { get; } = method;
    public int ResponseStatusCode { get; } = responseStatusCode;
    public SimulatorDataEntryPersistence Persistence { get; private set; } = persistence;
    public IEnumerable<string> QueryParameters { get; } = queryParameters;
    public string? BodyRegex { get; } = bodyRegex;
    public string? ResponseBody { get; } = responseBody;
    public string? ResponseContentType { get; } = responseContentType;
    public string? Tape { get; } = tape;
    public byte[]? File { get; private set; } = file;
    public string? FileName { get; private set; } = fileName;

    public void DecreasePersistence()
    {
        if (Persistence != SimulatorDataEntryPersistence.Always)
        {
            Persistence = (SimulatorDataEntryPersistence)((int)Persistence - 1);
        }
    }
}
