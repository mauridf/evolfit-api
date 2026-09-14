namespace EvolFit.Application.Features.Workouts.DTOs;

// ---------- Requests ----------
public sealed record GenerateWorkoutRequest(
    string Name,
    string Goal,
    int PeriodDays,
    List<string> BodyParts,
    string Difficulty);

public sealed record UpdateWorkoutStatusRequest(int Status);

public sealed record LogExerciseRequest(
    int WorkoutExerciseId,
    DateOnly Date,
    bool Completed,
    decimal? WeightUsed);

// ---------- Responses ----------
public sealed record WorkoutExerciseDto(
    int Id,
    int DayNumber,
    string ExerciseName,
    int WgerExerciseId,
    int Sets,
    int Reps,
    decimal? Weight,
    int? OrderInDay);

public sealed record WorkoutRoutineDetailResponse(
    int Id,
    string Name,
    string? Goal,
    DateOnly StartDate,
    DateOnly EndDate,
    int Status,
    int TotalDays,
    int TotalExercises,
    IReadOnlyList<WorkoutExerciseDto> Exercises);

public sealed record WorkoutRoutineListItem(
    int Id,
    string Name,
    string? Goal,
    DateOnly StartDate,
    DateOnly EndDate,
    int Status,
    int TotalDays,
    int TotalExercises,
    DateTime CreatedAt);

public sealed record WorkoutRoutineGenerateResponse(
    int Id,
    string Name,
    string? Goal,
    DateOnly StartDate,
    DateOnly EndDate,
    int Status,
    int TotalDays,
    int TotalExercises,
    IReadOnlyList<WorkoutExerciseDto> Exercises);

public sealed record TodayExerciseDto(
    int Id,
    string ExerciseName,
    int WgerExerciseId,
    int Sets,
    int Reps,
    decimal? Weight,
    int? OrderInDay,
    bool Completed);

public sealed record TodayResponse(
    string RoutineName,
    int DayNumber,
    DateOnly Date,
    IReadOnlyList<TodayExerciseDto> Exercises,
    decimal CompletionPercent);

public sealed record WorkoutProgressResponse(
    int RoutineId,
    int TotalExercises,
    int CompletedExercises,
    decimal CompletionPercent,
    int DaysCompleted,
    int TotalDays);
