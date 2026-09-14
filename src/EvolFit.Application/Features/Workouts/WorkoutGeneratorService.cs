using System.Text.Json;
using EvolFit.Application.Features.Wger.DTOs;
using EvolFit.Application.Features.Wger.Interfaces;
using EvolFit.Application.Features.Workouts.DTOs;
using EvolFit.Application.Features.Workouts.Mappers;
using EvolFit.Core.Entities;
using EvolFit.Core.Enums;
using Microsoft.Extensions.Logging;

namespace EvolFit.Application.Features.Workouts;

public interface IWorkoutGeneratorService
{
    /// <summary>
    /// Constrói a rotina (entidade + exercícios) sem persistir.
    /// A persistência é feita pelo WorkoutService.
    /// </summary>
    Task<(WorkoutRoutine Routine, List<WorkoutExercise> Exercises)> GenerateAsync(
        int userId, GenerateWorkoutRequest request, CancellationToken ct = default);
}

public class WorkoutGeneratorService : IWorkoutGeneratorService
{
    private const int ExercisesPerDay = 3;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    private readonly IWgerExerciseClient _wger;
    private readonly IWgerExerciseCacheRepository _cache;
    private readonly ILogger<WorkoutGeneratorService> _logger;

    public WorkoutGeneratorService(
        IWgerExerciseClient wger,
        IWgerExerciseCacheRepository cache,
        ILogger<WorkoutGeneratorService> logger)
    {
        _wger = wger;
        _cache = cache;
        _logger = logger;
    }

    public async Task<(WorkoutRoutine Routine, List<WorkoutExercise> Exercises)> GenerateAsync(
        int userId, GenerateWorkoutRequest request, CancellationToken ct = default)
    {
        var bodyParts = request.BodyParts
            .Select(BodyPartExtensions.FromApiValue)
            .ToList();

        var goal = GoalExtensions.FromApiValue(request.Goal);
        var difficulty = DifficultyExtensions.FromApiValue(request.Difficulty);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Cria a rotina (validação de período está na entidade)
        var routine = WorkoutRoutine.Create(userId, request.Name, goal, startDate, request.PeriodDays);

        // 2. Busca exercícios por parte do corpo
        var allExercises = new List<ExerciseListItemDto>();

        foreach (var part in bodyParts)
        {
            try
            {
                // WGR-003: cardio não é músculo — usa a categoria 15 da wger.
                IReadOnlyList<ExerciseListItemDto> exercises = part == BodyPart.Cardio
                    ? await _wger.GetExercisesByCategoryAsync(BodyPartMuscleMapper.CardioCategoryId, ct)
                    : await _wger.GetExercisesByMuscleAsync(BodyPartMuscleMapper.ToMuscleId(part), ct);

                allExercises.AddRange(exercises);

                // WGR-002: grava exercícios obtidos no cache local (offline + menos chamadas)
                foreach (var exercise in exercises)
                    await UpsertCacheAsync(exercise, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao buscar exercícios para parte do corpo {BodyPart}", part);
            }
        }

        // 3. Remove duplicatas por Id
        allExercises = allExercises
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .ToList();

        if (allExercises.Count == 0)
            throw new InvalidOperationException(
                "Não foi possível obter exercícios da wger. Tente novamente mais tarde (WGR-004).");

        _logger.LogInformation("Gerando rotina {Difficulty} com {Count} exercícios únicos distribuídos em {Days} dias",
            difficulty, allExercises.Count, request.PeriodDays);

        // 4. Prescrição de séries/repetições conforme a dificuldade
        var (sets, reps) = PrescribeByDifficulty(difficulty);

        // 5. Distribui exercícios entre os dias (round-robin)
        var workoutExercises = new List<WorkoutExercise>();
        var index = 0;

        for (var day = 1; day <= request.PeriodDays; day++)
        {
            for (var slot = 1; slot <= ExercisesPerDay; slot++)
            {
                var picked = allExercises[index % allExercises.Count];
                index++;

                var exercise = WorkoutExercise.Create(
                dayNumber: day,
                wgerExerciseId: picked.Id,
                exerciseName: picked.Name,
                sets: sets,
                reps: reps,
                orderInDay: slot);

                workoutExercises.Add(exercise);
            }
        }

        return (routine, workoutExercises);
    }

    private async Task UpsertCacheAsync(ExerciseListItemDto item, CancellationToken ct)
    {
        var musclesJson = JsonSerializer.Serialize(item.Muscles);
        var equipmentJson = JsonSerializer.Serialize(item.Equipment);

        var cached = await _cache.GetByWgerIdAsync(item.Id, ct);

        if (cached is null)
        {
            await _cache.AddAsync(WgerExerciseCache.Create(
                item.Id,
                item.Name,
                item.Description,
                item.Category,
                musclesJson,
                equipmentJson,
                imagesJson: null,
                CacheTtl), ct);
        }
        else if (cached.IsExpired)
        {
            cached.Refresh(
                item.Name,
                item.Description,
                item.Category,
                musclesJson,
                equipmentJson,
                imagesJson: null,
                CacheTtl);
            _cache.Update(cached);
        }
    }

    private static (int Sets, int Reps) PrescribeByDifficulty(Difficulty difficulty) => difficulty switch
    {
        // Reps no padrão documentado (MASTER_SPEC §17.3): 10-12
        Difficulty.Beginner => (Sets: 3, Reps: 10),
        Difficulty.Intermediate => (Sets: 3, Reps: 10),
        Difficulty.Advanced => (Sets: 4, Reps: 12),
        _ => (Sets: 3, Reps: 10)
    };
}
