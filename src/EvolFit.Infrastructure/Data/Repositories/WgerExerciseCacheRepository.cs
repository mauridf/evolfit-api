using EvolFit.Application.Features.Wger.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EvolFit.Infrastructure.Data.Repositories;

public class WgerExerciseCacheRepository : IWgerExerciseCacheRepository
{
    private readonly EvolFitDbContext _context;

    public WgerExerciseCacheRepository(EvolFitDbContext context) => _context = context;

    public Task<WgerExerciseCache?> GetByWgerIdAsync(int wgerExerciseId, CancellationToken ct = default) =>
        _context.WgerExercisesCache.FirstOrDefaultAsync(e => e.WgerExerciseId == wgerExerciseId, ct);

    public async Task<IReadOnlyList<WgerExerciseCache>> GetByMuscleIdAsync(
        int muscleId, CancellationToken ct = default)
    {
        var rows = await _context.WgerExercisesCache
            .Where(e => e.MuscleId == muscleId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

        return rows;
    }

    public async Task<IReadOnlyList<WgerExerciseCache>> GetByCategoryIdAsync(
        int categoryId, CancellationToken ct = default)
    {
        var rows = await _context.WgerExercisesCache
            .Where(e => e.CategoryId == categoryId)
            .OrderBy(e => e.Id)
            .ToListAsync(ct);

        return rows;
    }

    public async Task<IReadOnlyList<WgerExerciseCache>> SearchByTermsAsync(
        IReadOnlyCollection<string> terms, string languageMode, int limit, CancellationToken ct = default)
    {
        var cleanTerms = terms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToArray();

        IQueryable<WgerExerciseCache> query = _context.WgerExercisesCache;

        if (cleanTerms.Length > 0)
        {
            query = languageMode switch
            {
                "english" => query.Where(e =>
                    cleanTerms.Any(t => EF.Functions.ILike(e.Name, $"%{t}%"))),
                "portuguese" => query.Where(e =>
                    e.NamePt != null && cleanTerms.Any(t => EF.Functions.ILike(e.NamePt, $"%{t}%"))),
                _ => query.Where(e =>
                    cleanTerms.Any(t => EF.Functions.ILike(e.Name, $"%{t}%"))
                    || (e.NamePt != null && cleanTerms.Any(t => EF.Functions.ILike(e.NamePt, $"%{t}%")))),
            };
        }
        else
        {
            // sem termos válidos → nenhum resultado
            query = query.Where(_ => false);
        }

        var rows = await query
            .AsNoTracking()
            .OrderBy(e => e.Name)
            .ThenBy(e => e.WgerExerciseId)
            .Take(limit)
            .ToListAsync(ct);

        return rows;
    }

    public Task<DateTime?> MaxCatalogExpiresAtAsync(CancellationToken ct = default) =>
        _context.WgerExercisesCache
            .Where(e => e.CategoryId != null)
            .MaxAsync(e => (DateTime?)e.ExpiresAt, ct);

    public async Task RemoveCatalogEntriesAsync(CancellationToken ct = default)
    {
        var catalogRows = _context.WgerExercisesCache.Where(e => e.CategoryId != null);
        _context.WgerExercisesCache.RemoveRange(catalogRows);
        await Task.CompletedTask;
    }

    public async Task AddAsync(WgerExerciseCache entry, CancellationToken ct = default) =>
        await _context.WgerExercisesCache.AddAsync(entry, ct);

    public void Update(WgerExerciseCache entry) => _context.WgerExercisesCache.Update(entry);
}