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

    public async Task AddAsync(WgerExerciseCache entry, CancellationToken ct = default) =>
        await _context.WgerExercisesCache.AddAsync(entry, ct);

    public void Update(WgerExerciseCache entry) => _context.WgerExercisesCache.Update(entry);
}
