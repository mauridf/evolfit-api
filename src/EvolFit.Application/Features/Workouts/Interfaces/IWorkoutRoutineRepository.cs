using EvolFit.Core.Entities;
using EvolFit.Core.Enums;

namespace EvolFit.Application.Features.Workouts.Interfaces;

public interface IWorkoutRoutineRepository
{
    Task AddAsync(WorkoutRoutine routine, CancellationToken ct = default);
    Task<WorkoutRoutine?> GetByIdForUserAsync(int id, int userId, CancellationToken ct = default);
    Task<WorkoutRoutine?> GetWithExercisesForUserAsync(int id, int userId, CancellationToken ct = default);
    Task<WorkoutRoutine?> GetActiveForUserAsync(int userId, CancellationToken ct = default);
    Task<(IReadOnlyList<WorkoutRoutine> Items, int Total)> GetPagedForUserAsync(
        int userId, int page, int pageSize, WorkoutStatus? status, CancellationToken ct = default);
    void Update(WorkoutRoutine routine);
    void Delete(WorkoutRoutine routine);
}
