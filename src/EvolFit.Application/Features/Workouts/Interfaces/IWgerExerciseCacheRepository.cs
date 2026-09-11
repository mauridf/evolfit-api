using EvolFit.Core.Entities;

namespace EvolFit.Application.Features.Wger.Interfaces;

public interface IWgerExerciseCacheRepository
{
    Task<WgerExerciseCache?> GetByWgerIdAsync(int wgerExerciseId, CancellationToken ct = default);
    Task AddAsync(WgerExerciseCache entry, CancellationToken ct = default);
    void Update(WgerExerciseCache entry);
}
