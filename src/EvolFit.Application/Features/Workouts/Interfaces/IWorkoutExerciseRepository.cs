using EvolFit.Core.Entities;

namespace EvolFit.Application.Features.Workouts.Interfaces;

public interface IWorkoutExerciseRepository
{
    Task AddRangeAsync(IEnumerable<WorkoutExercise> exercises, CancellationToken ct = default);
    Task<WorkoutExercise?> GetByIdWithRoutineAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<WorkoutExercise>> GetByRoutineAndDayAsync(
        int routineId, int dayNumber, CancellationToken ct = default);
    Task<int> CountByRoutineAsync(int routineId, CancellationToken ct = default);
}
