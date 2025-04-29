using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sim.Api.Tests;

[TestClass]
public class RouteSelectionTests : SimulationTestBase
{
    [TestInitialize]
    public void SetUp()
    {
        base.SetUp();
    }

    [TestMethod]
    public async Task TestRouteSelection()
    {
        await AddSimulatorEntry(new SimulationsDataEntryModel("/api/test", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "Test response", responseContentType: "text/plain"));

        var response = await SimulatorClient.GetAsync("/api/test");

        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual("Test response", await response.Content.ReadAsStringAsync(), "Response content should match");
    }

    [TestMethod]
    public async Task TestRouteSelection_MultipleRoutes()
    {
        await AddSimulatorEntry(new SimulationsDataEntryModel("/api/test", "GET",
            SimulationsDataEntryModel.PersistenceTwice,
            SimulationsDataEntryModel.ResponseOK200, "Twice response", responseContentType: "text/plain"));

        await AddSimulatorEntry(new SimulationsDataEntryModel("/api/test", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "First response", responseContentType: "text/plain"));

        var response = await SimulatorClient.GetAsync("/api/test");

        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual("First response", await response.Content.ReadAsStringAsync(), "Response content should match");

        response = await SimulatorClient.GetAsync("/api/test");

        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual("Twice response", await response.Content.ReadAsStringAsync(), "Response content should match");

        response = await SimulatorClient.GetAsync("/api/test");

        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual("Twice response", await response.Content.ReadAsStringAsync(), "Response content should match");

        response = await SimulatorClient.GetAsync("/api/test");

        Assert.AreEqual((int)response.StatusCode, 400);
        Assert.Contains("Out of tape", await response.Content.ReadAsStringAsync(), "Response should be out of tape");
    }

    [TestMethod]
    public async Task TestRouteSelectionWithParameters()
    {
        await AddSimulatorEntry(new SimulationsDataEntryModel("/api/test?param1=1", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "Param 1", responseContentType: "text/plain"));

        await AddSimulatorEntry(new SimulationsDataEntryModel("/api/test?param1=1&param2", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "Param 2", responseContentType: "text/plain"));

        await AddSimulatorEntry(new SimulationsDataEntryModel("/api/test?param1=1&param2=2&param3=3", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "Param 3", responseContentType: "text/plain"));

        var response = await SimulatorClient.GetAsync("/api/test?param3=3");

        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual("Param 3", await response.Content.ReadAsStringAsync(), "Response content should match");

        // Response 1 was added first, so it should be returned
        response = await SimulatorClient.GetAsync("/api/test?param3=3");
        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual("Param 1", await response.Content.ReadAsStringAsync(), "Response content should match");

        // Response 2 was added after that, so it should be returned
        response = await SimulatorClient.GetAsync("/api/test?param3=3");
        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual("Param 2", await response.Content.ReadAsStringAsync(), "Response content should match");
    }

    [TestMethod]
    public async Task TestRouteSelectionWithParameters_MostMatches()
    {
        await AddSimulatorEntry(new SimulationsDataEntryModel("/api/test?param1=1", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "Entry 1", responseContentType: "text/plain"));

        await AddSimulatorEntry(new SimulationsDataEntryModel("/api/test?param1=1&param2=2&param3=3", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "Entry 2", responseContentType: "text/plain"));

        await AddSimulatorEntry(new SimulationsDataEntryModel("/api/test?param1=1&param2=2", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200, "Entry 3", responseContentType: "text/plain"));

        var response = await SimulatorClient.GetAsync("/api/test?param1=1&param2=2&param3=3");

        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual("Entry 2", await response.Content.ReadAsStringAsync(), "Response content should match");

        response = await SimulatorClient.GetAsync("/api/test?param1=1&param2=2&param3=3");

        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual("Entry 3", await response.Content.ReadAsStringAsync(), "Response content should match");

        response = await SimulatorClient.GetAsync("/api/test?param1=1&param2=2&param3=3");

        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual("Entry 1", await response.Content.ReadAsStringAsync(), "Response content should match");
    }
}
