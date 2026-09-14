using EvolFit.Application.Common;
using EvolFit.Application.Common.Exceptions;
using EvolFit.Application.Features.Health.DTOs;
using EvolFit.Application.Features.Workouts.DTOs;
using EvolFit.Application.Features.Workouts.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Core.Enums;

namespace EvolFit.Application.Features.Workouts;

public interface IWorkoutService
{
    Task<WorkoutRoutineGenerateResponse> GenerateAsync(
        GenerateWorkoutRequest request, CancellationToken ct = default);
    Task<PagedResponse<WorkoutRoutineListItem>> ListAsync(
        int page, int pageSize, string? status, CancellationToken ct = default);
    Task<WorkoutRoutineDetailResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<WorkoutRoutineDetailResponse> UpdateStatusAsync(
        int id, UpdateWorkoutStatusRequest request, CancellationToken ct = default);
    Task<TodayResponse> GetTodayAsync(CancellationToken ct = default);
    Task LogExerciseAsync(LogExerciseRequest request, CancellationToken ct = default);
    Task<WorkoutProgressResponse> GetProgressAsync(int id, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class WorkoutService : IWorkoutService
{
    private readonly IWorkoutRoutineRepository _routines;
    private readonly IWorkoutExerciseRepository _exercises;
    private readonly IExerciseLogRepository _logs;
    private readonly IWorkoutGeneratorService _generator;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    public WorkoutService(
        IWorkoutRoutineRepository routines,
        IWorkoutExerciseRepository exercises,
        IExerciseLogRepository logs,
        IWorkoutGeneratorService generator,
        IUnitOfWork uow,
        ICurrentUserService currentUser)
    {
        _routines = routines;
        _exercises = exercises;
        _logs = logs;
        _generator = generator;
        _uow = uow;
        _currentUser = currentUser;
    }

    public async Task<WorkoutRoutineGenerateResponse> GenerateAsync(
        GenerateWorkoutRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        // RN-005: uma única rotina ativa por usuário
        var active = await _routines.GetActiveForUserAsync(userId, ct);
        if (active is not null)
            throw new ConflictException(
                "Você já possui uma rotina ativa. Conclua ou exclua antes de gerar outra (RN-005).");

        var (routine, exercises) = await _generator.GenerateAsync(userId, request, ct);

        // Associa exercícios à rotina antes de persistir
        foreach (var ex in exercises)
            routine.Exercises.Add(ex);

        await _routines.AddAsync(routine, ct);
        await _uow.SaveChangesAsync(ct);

        return MapToGenerateResponse(routine);
    }

    public async Task<PagedResponse<WorkoutRoutineListItem>> ListAsync(
        int page, int pageSize, string? status, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _routines.GetPagedForUserAsync(
            _currentUser.UserId, page, pageSize, ParseStatusFilter(status), ct);

        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new PagedResponse<WorkoutRoutineListItem>(
            items.Select(MapToListItem).ToList(),
            page,
            pageSize,
            total,
            totalPages);
    }

    public async Task<WorkoutRoutineDetailResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var routine = await _routines.GetWithExercisesForUserAsync(id, _currentUser.UserId, ct)
            ?? throw new NotFoundException("Rotina não encontrada.");

        return MapToDetail(routine);
    }

    public async Task<WorkoutRoutineDetailResponse> UpdateStatusAsync(
        int id, UpdateWorkoutStatusRequest request, CancellationToken ct = default)
    {
        var routine = await _routines.GetWithExercisesForUserAsync(id, _currentUser.UserId, ct)
            ?? throw new NotFoundException("Rotina não encontrada.");

        if (!Enum.IsDefined(typeof(WorkoutStatus), request.Status))
            throw new Common.Exceptions.ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["Status inválido. Use 0 (paused), 1 (active) ou 2 (completed)."]
            });

        // RN-005: ativar uma rotina enquanto outra já está ativa violaria a
        // invariante (índice único parcial V010) — detecta e responde 409.
        if (request.Status == (int)WorkoutStatus.Active
            && routine.Status != WorkoutStatus.Active)
        {
            var active = await _routines.GetActiveForUserAsync(_currentUser.UserId, ct);
            if (active is not null)
                throw new ConflictException(
                    "Você já possui outra rotina ativa. Pause ou conclua a rotina ativa antes de ativar esta (RN-005).");
        }

        routine.ChangeStatus((WorkoutStatus)request.Status);
        _routines.Update(routine);
        await _uow.SaveChangesAsync(ct);

        return MapToDetail(routine);
    }

    public async Task<TodayResponse> GetTodayAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var routine = await _routines.GetActiveForUserAsync(userId, ct)
            ?? throw new NotFoundException("Nenhuma rotina ativa encontrada.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dayNumber = today.DayNumber - routine.StartDate.DayNumber + 1;

        if (dayNumber < 1 || dayNumber > routine.TotalDays)
            throw new NotFoundException("Hoje está fora do período da rotina ativa.");

        var exercises = await _exercises.GetByRoutineAndDayAsync(routine.Id, dayNumber, ct);
        var exerciseIds = exercises.Select(e => e.Id).ToList();

        var logs = await _logs.GetByUserAndExerciseIdsAsync(userId, exerciseIds, ct);
        var logsByExercise = logs
            .Where(l => l.Date == today)
            .ToDictionary(l => l.WorkoutExerciseId, l => l);

        var dto = exercises.Select(e =>
        {
            logsByExercise.TryGetValue(e.Id, out var log);
            return new TodayExerciseDto(
                e.Id,
                e.ExerciseName,
                e.WgerExerciseId,
                e.Sets,
                e.Reps,
                e.Weight,
                e.OrderInDay,
                log?.Completed ?? false);
        }).ToList();

        var completed = dto.Count(d => d.Completed);
        var percent = dto.Count == 0 ? 0m : Math.Round(100m * completed / dto.Count, 2);

        return new TodayResponse(routine.Name, dayNumber, today, dto, percent);
    }

    public async Task LogExerciseAsync(LogExerciseRequest request, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;

        var exercise = await _exercises.GetByIdWithRoutineAsync(request.WorkoutExerciseId, ct)
            ?? throw new NotFoundException("Exercício não encontrado.");

        // Garante isolamento (ARQ-001): o exercício pertence à rotina do usuário
        if (exercise.WorkoutRoutine is null || exercise.WorkoutRoutine.UserId != userId)
            throw new NotFoundException("Exercício não encontrado.");

        var existing = await _logs.GetByUserExerciseDateAsync(
            userId, request.WorkoutExerciseId, request.Date, ct);

        if (existing is null)
        {
            var log = ExerciseLog.Create(
                userId,
                request.WorkoutExerciseId,
                request.Date,
                request.Completed);

            await _logs.AddAsync(log, ct);
        }
        else
        {
            existing.MarkCompleted(request.Completed);
            _logs.Update(existing);
        }

        if (request.WeightUsed.HasValue)
        {
            exercise.SetWeight(request.WeightUsed.Value);
        }

        await _uow.SaveChangesAsync(ct);
    }

    public async Task<WorkoutProgressResponse> GetProgressAsync(int id, CancellationToken ct = default)
    {
        var routine = await _routines.GetWithExercisesForUserAsync(id, _currentUser.UserId, ct)
            ?? throw new NotFoundException("Rotina não encontrada.");

        var total = routine.Exercises.Count;
        var completed = await _logs.CountCompletedForRoutineAsync(_currentUser.UserId, id, ct);

        var percent = total == 0 ? 0m : Math.Round(100m * completed / total, 2);

        // Dias completos: dias em que todos os exercícios do dia foram concluídos
        var daysCompleted = 0;
        for (var day = 1; day <= routine.TotalDays; day++)
        {
            var dayExercises = routine.Exercises.Where(e => e.DayNumber == day).ToList();
            if (dayExercises.Count == 0) continue;

            var dayLogs = await _logs.GetByUserAndExerciseIdsAsync(
                _currentUser.UserId, dayExercises.Select(e => e.Id).ToList(), ct);

            if (dayLogs.Count(l => l.Completed) == dayExercises.Count)
                daysCompleted++;
        }

        return new WorkoutProgressResponse(
            id,
            total,
            completed,
            percent,
            daysCompleted,
            routine.TotalDays);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var routine = await _routines.GetByIdForUserAsync(id, _currentUser.UserId, ct)
            ?? throw new NotFoundException("Rotina não encontrada.");

        _routines.Delete(routine);
        await _uow.SaveChangesAsync(ct);
    }

    // ---------- Helpers ----------
    private static WorkoutStatus? ParseStatusFilter(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        if (Enum.TryParse<WorkoutStatus>(status, ignoreCase: true, out var parsed)
            && Enum.IsDefined(typeof(WorkoutStatus), parsed))
            return parsed;

        if (int.TryParse(status, out var numeric)
            && Enum.IsDefined(typeof(WorkoutStatus), numeric))
            return (WorkoutStatus)numeric;

        throw new Common.Exceptions.ValidationException(new Dictionary<string, string[]>
        {
            ["status"] = ["Status inválido. Use 'active', 'paused' ou 'completed'."]
        });
    }

    // ---------- Mappers ----------
    private static WorkoutExerciseDto MapExercise(WorkoutExercise e) =>
        new(e.Id, e.DayNumber, e.ExerciseName, e.WgerExerciseId,
            e.Sets, e.Reps, e.Weight, e.OrderInDay);

    private static WorkoutRoutineDetailResponse MapToDetail(WorkoutRoutine r) =>
        new(r.Id, r.Name, r.Goal?.ToApiValue(),
            r.StartDate, r.EndDate, (int)r.Status,
            r.TotalDays, r.Exercises.Count,
            r.Exercises.OrderBy(e => e.DayNumber).ThenBy(e => e.OrderInDay)
                .Select(MapExercise).ToList());

    private static WorkoutRoutineGenerateResponse MapToGenerateResponse(WorkoutRoutine r) =>
        new(r.Id, r.Name, r.Goal?.ToApiValue(),
            r.StartDate, r.EndDate, (int)r.Status,
            r.TotalDays, r.Exercises.Count,
            r.Exercises.OrderBy(e => e.DayNumber).ThenBy(e => e.OrderInDay)
                .Select(MapExercise).ToList());

    private static WorkoutRoutineListItem MapToListItem(WorkoutRoutine r) =>
        new(r.Id, r.Name, r.Goal?.ToApiValue(),
            r.StartDate, r.EndDate, (int)r.Status,
            r.TotalDays, r.Exercises.Count, r.CreatedAt);
}
