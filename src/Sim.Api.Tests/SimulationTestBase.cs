using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sim.Api.Tests;

public class SimulationsDataEntryModel(
    string route,
    string method,
    string persistence,
    int responseStatusCode,
    string? responseBody = null,
    string? responseContentType = null,
    string? bodyRegex = "",
    byte[]? file = null,
    string? fileName = null,
    string? fileContentType = null,
    int? timeoutInMs = null)
{
    public static string TapeHeader = "X-Simulator-Tape";
    public static string PersistenceOnce = "Once";
    public static string PersistenceTwice = "Twice";
    public static string PersistenceAlways = "Always";
    public static string GETMethod = "GET";
    public static string POSTMethod = "POST";
    public static int ResponseOK200 = 200;
    public static int ResponseError504 = 504;
    public string Route { get; private set; } = route;
    public string Method { get; private set; } = method;
    public string? BodyRegex { get; private set; } = bodyRegex;
    public string Persistence { get; private set; } = persistence;
    public int ResponseStatusCode { get; private set; } = responseStatusCode;
    public string? ResponseBody { get; private set; } = responseBody;
    public string? ResponseContentType { get; private set; } = responseContentType;
    public byte[]? File { get; private set; } = file;
    public string? FileName { get; private set; } = fileName;
    public string? FileContentType { get; private set; } = fileContentType;
    public string? Tape { get; private set; }
    public int? TimeoutInMs { get; private set; } = timeoutInMs;

    internal void AddToTape(string tape)
    {
        if (string.IsNullOrEmpty(tape))
            throw new ArgumentNullException(nameof(tape), "tape cannot be null or empty");

        Tape = tape;
    }
}

public abstract class SimulationTestBase
{
    protected string Tape;
    protected HttpClient SimulatorClient;

    protected void SetUp(bool setupTape = true)
    {
        if (setupTape)
            Tape = Guid.NewGuid().ToString();
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddJsonFile("testsettings.json");

        var factory = new ApiSimulatorWebApplicationFactory<Program>(configurationBuilder.Build());
        SimulatorClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        SimulatorClient.DefaultRequestHeaders.Add(SimulationsDataEntryModel.TapeHeader, Tape);
    }

    protected async Task<HttpResponseMessage> AddSimulatorEntry(SimulationsDataEntryModel dataEntryModel)
    {
        if (!String.IsNullOrEmpty(Tape))
            dataEntryModel.AddToTape(Tape);

        var defaultJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };
        string json = JsonSerializer.Serialize(dataEntryModel, defaultJsonOptions);

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await SimulatorClient.PostAsync("/api/simulations", content);
    }

    protected async Task AddSimulatorFile(SimulationsDataEntryModel dataEntryModel)
    {
        if (!String.IsNullOrEmpty(Tape))
            dataEntryModel.AddToTape(Tape);

        var multipartContent = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(dataEntryModel.File);
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue(dataEntryModel.FileContentType ??
                                                             "application/octet-stream");
        multipartContent.Add(fileContent, "file", dataEntryModel.FileName ?? "file");
        multipartContent.Add(new StringContent(dataEntryModel.Route), "route");
        multipartContent.Add(new StringContent(dataEntryModel.Method), "method");
        multipartContent.Add(new StringContent(dataEntryModel.Persistence), "persistence");
        multipartContent.Add(new StringContent(dataEntryModel.FileName), "fileName");
        multipartContent.Add(new StringContent(dataEntryModel.ResponseStatusCode.ToString()), "responseStatusCode");
        multipartContent.Add(new StringContent(dataEntryModel.ResponseContentType), "fileContentType");
        if (!String.IsNullOrEmpty(dataEntryModel.Tape))
            multipartContent.Add(new StringContent(dataEntryModel.Tape), "tape");

        HttpResponseMessage response = await SimulatorClient.PostAsync("/api/simulations/file", multipartContent);
        if (!response.IsSuccessStatusCode)
        {
            var contentString = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to setup simulator response: {response.StatusCode}, {contentString}");
        }

        Assert.IsTrue(response.IsSuccessStatusCode, $"Failed to setup simulator response: {response.StatusCode}");
    }
}
