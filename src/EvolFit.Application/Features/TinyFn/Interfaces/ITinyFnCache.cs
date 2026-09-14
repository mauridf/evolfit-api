namespace EvolFit.Application.Features.TinyFn.Interfaces;

public interface ITinyFnCache
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken ct = default);
}
