using EvolFit.Application.Features.Health.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EvolFit.Infrastructure.Data.Repositories;

public class HealthMetricRepository : IHealthMetricRepository
{
    private readonly EvolFitDbContext _context;

    public HealthMetricRepository(EvolFitDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(HealthMetric metric, CancellationToken ct = default) =>
        await _context.HealthMetrics.AddAsync(metric, ct);

    public Task<HealthMetric?> GetByIdForUserAsync(int id, int userId, CancellationToken ct = default) =>
        _context.HealthMetrics.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId, ct);

    public Task<HealthMetric?> GetLatestForUserAsync(int userId, CancellationToken ct = default) =>
        _context.HealthMetrics
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.MeasuredAt)
            .FirstOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<HealthMetric> Items, int Total)> GetPagedForUserAsync(
        int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.HealthMetrics
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.MeasuredAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<HealthMetric>> GetEvolutionForUserAsync(
        int userId, int periodDays, CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-periodDays);

        return await _context.HealthMetrics
            .Where(m => m.UserId == userId && m.MeasuredAt >= cutoff)
            .OrderBy(m => m.MeasuredAt)
            .ToListAsync(ct);
    }

    public Task<int> CountForUserAsync(int userId, CancellationToken ct = default) =>
        _context.HealthMetrics.CountAsync(m => m.UserId == userId, ct);

    public void Delete(HealthMetric metric) => _context.HealthMetrics.Remove(metric);
}
