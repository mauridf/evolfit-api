using EvolFit.Application.Features.Workouts.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EvolFit.Infrastructure.Data.Repositories;

public class WorkoutExerciseRepository : IWorkoutExerciseRepository
{
    private readonly EvolFitDbContext _context;

    public WorkoutExerciseRepository(EvolFitDbContext context) => _context = context;

    public async Task AddRangeAsync(IEnumerable<WorkoutExercise> exercises, CancellationToken ct = default) =>
        await _context.WorkoutExercises.AddRangeAsync(exercises, ct);

    public Task<WorkoutExercise?> GetByIdWithRoutineAsync(int id, CancellationToken ct = default) =>
        _context.WorkoutExercises
            .Include(e => e.WorkoutRoutine)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<WorkoutExercise>> GetByRoutineAndDayAsync(
        int routineId, int dayNumber, CancellationToken ct = default) =>
        await _context.WorkoutExercises
            .Where(e => e.WorkoutRoutineId == routineId && e.DayNumber == dayNumber)
            .OrderBy(e => e.OrderInDay)
            .ToListAsync(ct);

    public Task<int> CountByRoutineAsync(int routineId, CancellationToken ct = default) =>
        _context.WorkoutExercises.CountAsync(e => e.WorkoutRoutineId == routineId, ct);
}
