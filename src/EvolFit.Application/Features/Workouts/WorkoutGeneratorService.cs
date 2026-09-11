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
    private const int DefaultSets = 3;
    private const int DefaultReps = 10;

    private readonly IWgerExerciseClient _wger;
    private readonly ILogger<WorkoutGeneratorService> _logger;

    public WorkoutGeneratorService(IWgerExerciseClient wger, ILogger<WorkoutGeneratorService> logger)
    {
        _wger = wger;
        _logger = logger;
    }

    public async Task<(WorkoutRoutine Routine, List<WorkoutExercise> Exercises)> GenerateAsync(
        int userId, GenerateWorkoutRequest request, CancellationToken ct = default)
    {
        var bodyParts = request.BodyParts
            .Select(BodyPartExtensions.FromApiValue)
            .ToList();

        var goal = GoalExtensions.FromApiValue(request.Goal);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // 1. Cria a rotina (validação de período está na entidade)
        var routine = WorkoutRoutine.Create(userId, request.Name, goal, startDate, request.PeriodDays);

        // 2. Busca exercícios por parte do corpo
        var muscleIds = BodyPartMuscleMapper.ToMuscleIds(bodyParts);
        var allExercises = new List<ExerciseListItemDto>();

        foreach (var muscleId in muscleIds)
        {
            try
            {
                var exercises = await _wger.GetExercisesByMuscleAsync(muscleId, ct);
                allExercises.AddRange(exercises);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Falha ao buscar exercícios para músculo {MuscleId}", muscleId);
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

        _logger.LogInformation("Gerando rotina com {Count} exercícios únicos distribuídos em {Days} dias",
            allExercises.Count, request.PeriodDays);

        // 4. Distribui exercícios entre os dias (round-robin)
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
                sets: DefaultSets,
                reps: DefaultReps,
                orderInDay: slot);

                workoutExercises.Add(exercise);
            }
        }

        return (routine, workoutExercises);
    }
}
