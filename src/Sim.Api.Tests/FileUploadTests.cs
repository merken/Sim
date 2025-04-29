using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sim.Api.Tests;

[TestClass]
public class FileUploadTests : SimulationTestBase
{
    [TestInitialize]
    public void SetUp()
    {
        base.SetUp();
    }

    [TestMethod]
    public async Task TestFileUpload()
    {
        var testFilePath = Path.Combine("TestFiles", "TestFile1.txt");
        var fileText = await File.ReadAllTextAsync(testFilePath);
        var fileBytes = await File.ReadAllBytesAsync(testFilePath);
        await AddSimulatorFile(new SimulationsDataEntryModel("/file.txt", "GET",
            SimulationsDataEntryModel.PersistenceOnce,
            SimulationsDataEntryModel.ResponseOK200,
            file: fileBytes,
            fileName: "testfile.txt",
            responseContentType: "text/plain"));

        var response = await SimulatorClient.GetAsync("/file.txt");
        Assert.IsTrue(response.IsSuccessStatusCode, "Response should be successful");
        Assert.AreEqual(fileText, await response.Content.ReadAsStringAsync(),
            "Response content should match");
    }
}
