using System.Text.RegularExpressions;

namespace Sim.Api;

public class EntryResult
{
    public bool HasResult { get; set; }
    public bool OutOfTape { get; set; }
    public string? Tape { get; set; }
    public Guid? Id { get; set; }

    public static EntryResult WithResult(Guid id) => new EntryResult { HasResult = true, Id = id };
    public static EntryResult NoResult() => new EntryResult { HasResult = false };
    public static EntryResult OutOfTapeResult(string tape) => new EntryResult { OutOfTape = true, Tape = tape };
}

public interface ISimulationsDataStore
{
    Task<IEnumerable<SimulatorDataEntry>> GetAllDataEntries(
        CancellationToken cancellationToken = default
    );

    Task<SimulatorDataEntry?> TryGetEntry(Guid id, CancellationToken cancellationToken = default);

    Task<EntryResult> FindEntry(
        string route,
        string method,
        string body,
        IQueryCollection queries,
        string? tape = null,
        CancellationToken cancellationToken = default
    );

    Task<Guid> AddEntry(SimulatorDataEntry entry, CancellationToken cancellationToken = default);
    Task DeleteEntry(Guid id, CancellationToken cancellationToken = default);
    Task DeleteAllEntries(string? tape, CancellationToken cancellationToken = default);
}

public class SimulationsDataStore : ISimulationsDataStore
{
    private readonly SimulatorCache<Guid> _cache = new();

    public async Task<IEnumerable<SimulatorDataEntry>> GetAllDataEntries(
        CancellationToken cancellationToken = default
    )
    {
        return _cache.Values;
    }

    public async Task<SimulatorDataEntry?> TryGetEntry(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        if (cancellationToken.IsCancellationRequested)
            return null;

        var entry = _cache.TryGet(id, out SimulatorDataEntry? value) ? value : null;

        return entry;
    }

    public async Task<EntryResult> FindEntry(
        string route,
        string method,
        string body,
        IQueryCollection queries,
        string? tape = null,
        CancellationToken cancellationToken = default
    )
    {
        if (cancellationToken.IsCancellationRequested)
            return EntryResult.NoResult();

        var routeToFind = route.Trim();
        if (!routeToFind.StartsWith('/'))
            routeToFind = $"/{routeToFind}";

        var entries = _cache.AsEnumerable();

        var tapeItems = entries.Where(e => !String.IsNullOrEmpty(e.Value.Tape) && e.Value.Tape == tape);
        var otherItems = entries.Where(e => String.IsNullOrEmpty(e.Value.Tape));

        if (!String.IsNullOrEmpty(tape))
        {
            entries = tapeItems;
            if (!entries.Any(e => e.Value.Tape == tape))
                return EntryResult.OutOfTapeResult(tape);
        }
        else
            entries = otherItems;

        entries = entries.Where(e => e.Value.Route == routeToFind && e.Value.Method == new HttpMethod(method)
        );


        if (queries.Count > 0)
        {
            var queryStrings = queries.Select(q => $"{q.Key}={q.Value}");
            var bestMatches = entries
                .Select(
                    // Find matches in the query parameters
                    e =>
                    {
                        var count = 0;
                        foreach (var entry in queryStrings)
                            foreach (var parameter in e.Value.QueryParameters)
                                if (String.Equals(parameter, entry, StringComparison.CurrentCultureIgnoreCase))
                                    count++;
                        return new { Count = count, Value = e };
                    })
                .Where(x => x.Count > 0)
                .OrderByDescending(x => x.Count)
                .Select(x => new { x.Count, Entry = x.Value });

            if (bestMatches.Any())
            {
                var countMatches = new Dictionary<int, List<KeyValuePair<Guid, SimulatorDataEntry>>>();
                foreach (var match in bestMatches)
                {
                    if (!countMatches.ContainsKey(match.Count))
                        countMatches[match.Count] = new List<KeyValuePair<Guid, SimulatorDataEntry>>();
                    countMatches[match.Count].Add(match.Entry);
                }

                // Run through the counts
                foreach (var matchingEntries in countMatches.Values)
                {
                    var orderedMatches = matchingEntries
                        .OrderBy(e => e.Value.Persistence)
                        .ThenBy(e => e.Value.Added)
                        .ToList();

                    if (!string.IsNullOrEmpty(body))
                    {
                        var bodyMatch = FindByBodyRegex(orderedMatches, body);
                        if (bodyMatch != null)
                            return EntryResult.WithResult(bodyMatch.Value);
                    }

                    var bestMatch = orderedMatches.FirstOrDefault();
                    if (bestMatch.Key != Guid.Empty)
                        return EntryResult.WithResult(bestMatch.Key);
                }
            }
        }

        if (!string.IsNullOrEmpty(body))
        {
            var bodyMatch = FindByBodyRegex(entries, body);
            if (bodyMatch != null)
                return EntryResult.WithResult(bodyMatch.Value);
        }

        var exact = entries
            .Where(e => string.IsNullOrEmpty(e.Value.BodyRegex))
            .OrderBy(e => e.Value.Persistence)
            .ThenBy(e => e.Value.Added)
            .FirstOrDefault();
        if (exact.Key != Guid.Empty)
            return EntryResult.WithResult(exact.Key);

        foreach (var cacheEntry in _cache)
        {
            if (String.IsNullOrEmpty(cacheEntry.Value.BodyRegex))
                continue; // skip if no regex is defined
            var regex = new Regex(cacheEntry.Value.BodyRegex);
            if (regex.IsMatch(routeToFind) && cacheEntry.Value.Method == new HttpMethod(method))
            {
                return EntryResult.WithResult(cacheEntry.Key);
            }
        }

        return EntryResult.NoResult();
    }

    private Guid? FindByBodyRegex(IEnumerable<KeyValuePair<Guid, SimulatorDataEntry>> entries, string body)
    {
        if (string.IsNullOrEmpty(body))
            return null;
        var possibleMatches = entries
            .Where(e => !string.IsNullOrWhiteSpace(e.Value.BodyRegex))
            .OrderBy(e => e.Value.Persistence)
            .ThenBy(e => e.Value.Added)
            .ToList();
        foreach (var entry in possibleMatches)
        {
            var regex = new Regex(entry.Value.BodyRegex);
            if (regex.IsMatch(body))
            {
                return entry.Key;
            }
        }

        return null;
    }

    public async Task<Guid> AddEntry(
        SimulatorDataEntry entry,
        CancellationToken cancellationToken = default
    )
    {
        if (cancellationToken.IsCancellationRequested)
            return Guid.Empty;

        var id = Guid.NewGuid();
        _cache.TryAdd(id, entry);

        return id;
    }

    public async Task DeleteEntry(Guid id, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        _cache.Remove(id);
    }

    public async Task DeleteAllEntries(string? tape, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        if (!string.IsNullOrEmpty(tape))
        {
            var entries = _cache.AsEnumerable();
            entries = entries.Where(e => e.Value.Tape == tape);
            foreach (var entry in entries)
                await DeleteEntry(entry.Key, cancellationToken);
        }
        else
            _cache.Clear();
    }
}
