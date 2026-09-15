using EvolFit.Application.Features.Wger.DTOs;

namespace EvolFit.Application.Features.Wger.Interfaces;

public interface IWgerExerciseClient
{
    Task<ExerciseSearchResponse> SearchExercisesAsync(string term, CancellationToken ct = default);
    Task<ExerciseListResponse> GetExercisesByMuscleAsync(int muscleId, CancellationToken ct = default);
    Task<ExerciseListResponse> GetExercisesByCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<ExerciseListResponse> GetExercisesByEquipmentAsync(int equipmentId, CancellationToken ct = default);
    Task<ExerciseDetailResponse> GetExerciseInfoAsync(int exerciseId, CancellationToken ct = default);
    Task<MuscleListResponse> GetMusclesAsync(CancellationToken ct = default);
    Task<CategoryListResponse> GetCategoriesAsync(CancellationToken ct = default);
}