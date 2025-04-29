using System.Collections;
using System.Collections.Concurrent;

namespace Sim.Api;

public class SimulatorCache<TKey>
    : IEnumerable<KeyValuePair<TKey, SimulatorDataEntry>>
{
    private readonly ConcurrentDictionary<TKey, SimulatorDataEntry> _cache = new();
    public int Count => _cache.Count;

    public void Clear() => _cache.Clear();

    public void AddOrUpdate(TKey key, SimulatorDataEntry value)
    {
        _cache.AddOrUpdate(key, (k, c) => c, (k, v, c) => c, value);
    }

    public bool TryGet(TKey key, out SimulatorDataEntry value)
    {
        value = null;

        if (!_cache.TryGetValue(key, out SimulatorDataEntry foundValue))
        {
            return false; //not found
        }

        value = foundValue;
        return true;
    }

    public bool TryAdd(TKey key, SimulatorDataEntry value)
    {
        if (TryGet(key, out _))
        {
            return false;
        }

        return _cache.TryAdd(key, value);
    }

    public SimulatorDataEntry GetOrAdd(TKey key, Func<TKey, SimulatorDataEntry> valueFactory)
    {
        if (TryGet(key, out var value))
        {
            return value;
        }

        return _cache
            .GetOrAdd(key, (k, v) => valueFactory(k), valueFactory);
    }

    public SimulatorDataEntry GetOrAdd(TKey key, SimulatorDataEntry value)
    {
        if (TryGet(key, out var existingValue))
        {
            return existingValue;
        }

        return _cache.GetOrAdd(key, value);
    }

    public void Remove(TKey key)
    {
        _cache.TryRemove(key, out _);
    }

    public IEnumerable<SimulatorDataEntry> Values => _cache.Values;

    public IEnumerator<KeyValuePair<TKey, SimulatorDataEntry>> GetEnumerator()
    {
        foreach (var cacheItem in _cache)
        {
            yield return new KeyValuePair<TKey, SimulatorDataEntry>(
                cacheItem.Key,
                cacheItem.Value
            );
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }
}
