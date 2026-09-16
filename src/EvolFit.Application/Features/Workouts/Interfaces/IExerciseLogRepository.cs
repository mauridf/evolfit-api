using EvolFit.Core.Entities;

namespace EvolFit.Application.Features.Workouts.Interfaces;

public interface IExerciseLogRepository
{
    Task AddAsync(ExerciseLog log, CancellationToken ct = default);
    Task<ExerciseLog?> GetByUserExerciseDateAsync(
        int userId, int workoutExerciseId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<ExerciseLog>> GetByUserAndDateAsync(
        int userId, DateOnly date, CancellationToken ct = default);
    Task<IReadOnlyList<ExerciseLog>> GetByUserAndDateRangeAsync(
        int userId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IReadOnlyList<ExerciseLog>> GetByUserAndExerciseIdsAsync(
        int userId, IReadOnlyCollection<int> exerciseIds, CancellationToken ct = default);
    Task<int> CountCompletedForRoutineAsync(int userId, int routineId, CancellationToken ct = default);
    void Update(ExerciseLog log);
}
