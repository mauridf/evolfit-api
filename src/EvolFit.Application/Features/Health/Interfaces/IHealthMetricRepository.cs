using EvolFit.Core.Entities;

namespace EvolFit.Application.Features.Health.Interfaces;

public interface IHealthMetricRepository
{
    Task AddAsync(HealthMetric metric, CancellationToken ct = default);
    Task<HealthMetric?> GetByIdForUserAsync(int id, int userId, CancellationToken ct = default);
    Task<HealthMetric?> GetLatestForUserAsync(int userId, CancellationToken ct = default);
    Task<(IReadOnlyList<HealthMetric> Items, int Total)> GetPagedForUserAsync(
        int userId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<HealthMetric>> GetEvolutionForUserAsync(
        int userId, int periodDays, CancellationToken ct = default);
    Task<int> CountForUserAsync(int userId, CancellationToken ct = default);
    void Delete(HealthMetric metric);
}
