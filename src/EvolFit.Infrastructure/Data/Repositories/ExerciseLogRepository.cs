using EvolFit.Application.Features.Workouts.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EvolFit.Infrastructure.Data.Repositories;

public class ExerciseLogRepository : IExerciseLogRepository
{
    private readonly EvolFitDbContext _context;

    public ExerciseLogRepository(EvolFitDbContext context) => _context = context;

    public async Task AddAsync(ExerciseLog log, CancellationToken ct = default) =>
        await _context.ExerciseLogs.AddAsync(log, ct);

    public Task<ExerciseLog?> GetByUserExerciseDateAsync(
        int userId, int workoutExerciseId, DateOnly date, CancellationToken ct = default) =>
        _context.ExerciseLogs.FirstOrDefaultAsync(
            l => l.UserId == userId
              && l.WorkoutExerciseId == workoutExerciseId
              && l.Date == date, ct);

    public async Task<IReadOnlyList<ExerciseLog>> GetByUserAndDateAsync(
        int userId, DateOnly date, CancellationToken ct = default) =>
        await _context.ExerciseLogs
            .Where(l => l.UserId == userId && l.Date == date)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ExerciseLog>> GetByUserAndDateRangeAsync(
        int userId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        await _context.ExerciseLogs
            .Where(l => l.UserId == userId && l.Date >= from && l.Date <= to)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ExerciseLog>> GetByUserAndExerciseIdsAsync(
        int userId, IReadOnlyCollection<int> exerciseIds, CancellationToken ct = default) =>
        await _context.ExerciseLogs
            .Where(l => l.UserId == userId && exerciseIds.Contains(l.WorkoutExerciseId))
            .ToListAsync(ct);

    public async Task<int> CountCompletedForRoutineAsync(
        int userId, int routineId, CancellationToken ct = default) =>
        await _context.ExerciseLogs
            .Where(l => l.UserId == userId
                     && l.Completed
                     && l.WorkoutExercise!.WorkoutRoutineId == routineId)
            .CountAsync(ct);

    public void Update(ExerciseLog log) => _context.ExerciseLogs.Update(log);
}
