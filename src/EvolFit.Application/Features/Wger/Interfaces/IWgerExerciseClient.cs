using EvolFit.Application.Features.Wger.DTOs;

namespace EvolFit.Application.Features.Wger.Interfaces;

public interface IWgerExerciseClient
{
    Task<ExerciseSearchResponse> SearchExercisesAsync(string term, CancellationToken ct = default);
    Task<IReadOnlyList<ExerciseListItemDto>> GetExercisesByMuscleAsync(int muscleId, CancellationToken ct = default);
    Task<ExerciseDetailDto> GetExerciseInfoAsync(int exerciseId, CancellationToken ct = default);
}
