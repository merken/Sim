using Microsoft.AspNetCore.Mvc;

namespace Sim.Api.Controllers;

[ApiController]
[Route("dynamic")]
[ApiExplorerSettings(IgnoreApi = true)]
public class DynamicController(ISimulationsDataStore simulationsDataStore, ILogger<DynamicController> logger)
    : Controller
{
    [Route("OutOfTape/{tape}")]
    public async Task<IActionResult> OutOfTape(string tape, CancellationToken cancellationToken)
    {
        throw new OutOfTapeException(tape);
    }

    [Route("GetEntry/{id}")]
    public async Task<IActionResult> GetEntry(Guid id, CancellationToken cancellationToken)
    {
        var entry = await simulationsDataStore.TryGetEntry(id, cancellationToken);

        if (entry == null)
        {
            logger.LogInformation("No entry found for ID: {id}", id);
            return NotFound();
        }

        if (entry.TimeoutInMs != null && entry.TimeoutInMs > 0)
        {
            logger.LogInformation("Simulating timeout {timeout} for ID: {id}", entry.TimeoutInMs, id);
            await Task.Delay(TimeSpan.FromMilliseconds(entry.TimeoutInMs.Value), cancellationToken);
        }

        entry.DecreasePersistence();

        if (entry.Persistence == SimulatorDataEntryPersistence.None)
            await simulationsDataStore.DeleteEntry(id, cancellationToken);

        if (entry.File != null)
            return File(entry.File, entry.ResponseContentType, entry.FileName);

        if (entry.ResponseBody == null)
            return StatusCode(entry.ResponseStatusCode);

        if (entry.ResponseContentType != null)
            HttpContext.Response.ContentType = entry.ResponseContentType;

        if (entry.ResponseStatusCode != 200)
            return StatusCode(entry.ResponseStatusCode, entry.ResponseBody);

        await HttpContext.Response.WriteAsync(entry.ResponseBody, cancellationToken);

        return new EmptyResult();
    }
}
