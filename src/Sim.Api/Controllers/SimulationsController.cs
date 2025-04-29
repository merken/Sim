using Microsoft.AspNetCore.Mvc;
using Sim.Api.Validators;

namespace Sim.Api.Controllers;

[ApiController]
[Route("api/simulations")]
public class SimulationsController(
    ISimulationsDataStore simulationsDataStore,
    IValidator<SimulationsDataEntryModel> validator,
    ILogger<SimulationsController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> GetSimulations(CancellationToken cancellationToken)
    {
        var data = await simulationsDataStore.GetAllDataEntries(cancellationToken);
        return Ok(data);
    }

    [HttpPost]
    public async Task<IActionResult> AddSimulation(
        [FromBody] SimulationsDataEntryModel model,
        CancellationToken cancellationToken
    )
    {
        await validator.Validate(model, cancellationToken);

        logger.LogInformation("Adding entry for {route} {method} {statusCode} {persistence} {tape}", model.Route,
            model.Method,
            model.ResponseStatusCode, model.Persistence, model.Tape);

        var id = await simulationsDataStore.AddEntry(
            SimulationsDataEntryModel.ToSimulatorDataEntry(model),
            cancellationToken
        );
        return Ok(id);
    }

    [HttpPost("file")]
    [ExcludeFromDescription]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> AddSimulationFile(
        [FromForm] IFormFile file,
        [FromForm] string route,
        [FromForm] string method,
        [FromForm] int responseStatusCode,
        [FromForm] string persistence,
        [FromForm] string fileName,
        [FromForm] string fileContentType,
        [FromForm] string? tape,
        CancellationToken cancellationToken
    )
    {
        var model = new SimulationsDataEntryModel
        {
            Route = route,
            Method = method,
            ResponseStatusCode = responseStatusCode,
            Persistence = persistence,
            FileName = fileName,
            ResponseContentType = fileContentType,
            Tape = tape
        };

        await validator.Validate(model, cancellationToken);

        if (file.Length < 0)
            throw new ValidationException($"file must be provided in the form");
        if (String.IsNullOrEmpty(model.FileName))
            throw new ValidationException($"fileName must be provided in the form");
        if (String.IsNullOrEmpty(model.ResponseContentType))
            throw new ValidationException($"fileContentType must be provided in the form");

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        model.File = stream.ToArray();

        logger.LogInformation("Adding file entry for {route} {method} {statusCode} {persistence} {tape}", model.Route,
            model.Method,
            model.ResponseStatusCode, model.Persistence, model.Tape);

        var id = await simulationsDataStore.AddEntry(
            SimulationsDataEntryModel.ToSimulatorDataEntry(model),
            cancellationToken
        );
        return Ok(id);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteSimulation(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting entry {id}", id);

        await simulationsDataStore.DeleteEntry(id, cancellationToken);

        return Ok();
    }

    [HttpDelete]
    [Route("purge")]
    public async Task<IActionResult> DeleteAllSimulations(string? tape, CancellationToken cancellationToken)
    {
        logger.LogInformation("Purging all entries for tape {tape}", tape);

        await simulationsDataStore.DeleteAllEntries(tape, cancellationToken);

        return Ok();
    }
}
