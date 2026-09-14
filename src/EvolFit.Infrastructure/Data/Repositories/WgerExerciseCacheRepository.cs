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

    public async Task AddAsync(WgerExerciseCache entry, CancellationToken ct = default) =>
        await _context.WgerExercisesCache.AddAsync(entry, ct);

    public void Update(WgerExerciseCache entry) => _context.WgerExercisesCache.Update(entry);
}
