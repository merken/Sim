using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sim.Api.Tests;

[TestClass]
public class ApiTests : SimulationTestBase
{
    [TestInitialize]
    public void SetUp()
    {
        base.SetUp(false);
    }

    [TestMethod]
    public async Task NotFound()
    {
        var response = await SimulatorClient.GetAsync("/api/doesnotexist");

        Assert.AreEqual((int)response.StatusCode, 404, "404 is expected");
    }

    [TestMethod]
    public async Task ValidationError_Route()
    {
        var response = await AddSimulatorEntry(new SimulationsDataEntryModel(null, "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "Test response", responseContentType: "text/plain"));

        Assert.AreEqual((int)response.StatusCode, 400, "Bad request is expected");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("The Route field is required", content, "Bad request is expected");
    }

    [TestMethod]
    public async Task ValidationError_RouteRoot()
    {
        var response = await AddSimulatorEntry(new SimulationsDataEntryModel("/", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "Test response", responseContentType: "text/plain"));

        Assert.AreEqual((int)response.StatusCode, 400, "Bad request is expected");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Route cannot be '/'", content, "Bad request is expected");
    }

    [TestMethod]
    public async Task ValidationError_Method()
    {
        var response = await AddSimulatorEntry(new SimulationsDataEntryModel("api/null", null,
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "Test response", responseContentType: "text/plain"));

        Assert.AreEqual((int)response.StatusCode, 400, "Bad request is expected");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("The Method field is required", content, "Bad request is expected");
    }

    [TestMethod]
    public async Task ValidationError_StatusCode()
    {
        var response = await AddSimulatorEntry(new SimulationsDataEntryModel("api/null", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            0, "Test response", responseContentType: "text/plain"));

        Assert.AreEqual((int)response.StatusCode, 400, "Bad request is expected");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ResponseStatusCode is not valid", content, "Bad request is expected");
    }

    [TestMethod]
    [DataRow("CONNECT")]
    [DataRow("TRACE")]
    public async Task ValidationError_MethodNotAllowed(string method)
    {
        var response = await AddSimulatorEntry(new SimulationsDataEntryModel("api/null", method,
            SimulationsDataEntryModel.PersistenceOnce,
            200, "Test response", responseContentType: "text/plain"));

        Assert.AreEqual((int)response.StatusCode, 400, "Bad request is expected");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains($"Method '{method}' is not allowed", content, "Bad request is expected");
    }

    [TestMethod]
    [DataRow("/swagger")]
    [DataRow("/swagger/index.html")]
    [DataRow("/api/simulations")]
    [DataRow("/api/simulations/purge")]
    [DataRow("/api/simulations/file")]
    public async Task ValidationError_RouteNotAllowed(string route)
    {
        var response = await AddSimulatorEntry(new SimulationsDataEntryModel(route,
            "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            200, "Test response", responseContentType: "text/plain"));

        Assert.AreEqual((int)response.StatusCode, 400, "Bad request is expected");
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains($"Route '{route}' is not allowed", content, "Bad request is expected");
    }
}
