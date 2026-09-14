using EvolFit.Core.Entities;

namespace EvolFit.Application.Features.Wger.Interfaces;

public interface IWgerExerciseCacheRepository
{
    Task<WgerExerciseCache?> GetByWgerIdAsync(int wgerExerciseId, CancellationToken ct = default);
    Task<IReadOnlyList<WgerExerciseCache>> GetByMuscleIdAsync(int muscleId, CancellationToken ct = default);
    Task<IReadOnlyList<WgerExerciseCache>> GetByCategoryIdAsync(int categoryId, CancellationToken ct = default);
    Task AddAsync(WgerExerciseCache entry, CancellationToken ct = default);
    void Update(WgerExerciseCache entry);
}
