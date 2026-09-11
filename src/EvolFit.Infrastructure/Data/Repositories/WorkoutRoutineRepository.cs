using EvolFit.Application.Features.Workouts.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Core.Enums;
using EvolFit.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace EvolFit.Infrastructure.Data.Repositories;

public class WorkoutRoutineRepository : IWorkoutRoutineRepository
{
    private readonly EvolFitDbContext _context;

    public WorkoutRoutineRepository(EvolFitDbContext context) => _context = context;

    public async Task AddAsync(WorkoutRoutine routine, CancellationToken ct = default) =>
        await _context.WorkoutRoutines.AddAsync(routine, ct);

    public Task<WorkoutRoutine?> GetByIdForUserAsync(int id, int userId, CancellationToken ct = default) =>
        _context.WorkoutRoutines.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

    public Task<WorkoutRoutine?> GetWithExercisesForUserAsync(int id, int userId, CancellationToken ct = default) =>
        _context.WorkoutRoutines
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

    public Task<WorkoutRoutine?> GetActiveForUserAsync(int userId, CancellationToken ct = default) =>
        _context.WorkoutRoutines
            .Where(r => r.UserId == userId && r.Status == WorkoutStatus.Active)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<(IReadOnlyList<WorkoutRoutine> Items, int Total)> GetPagedForUserAsync(
        int userId, int page, int pageSize, int? status, CancellationToken ct = default)
    {
        var query = _context.WorkoutRoutines
            .Include(r => r.Exercises)
            .Where(r => r.UserId == userId);

        if (status.HasValue)
            query = query.Where(r => (int)r.Status == status.Value);

        query = query.OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public void Update(WorkoutRoutine routine) => _context.WorkoutRoutines.Update(routine);

    public void Delete(WorkoutRoutine routine) => _context.WorkoutRoutines.Remove(routine);
}
