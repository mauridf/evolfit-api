namespace EvolFit.Core.Enums;

/// <summary>
/// Objetivo principal da rotina de treinos.
/// </summary>
public enum Goal
{
    Strength = 1,
    Hypertrophy = 2,
    Endurance = 3,
    Flexibility = 4,
    Cardio = 5
}

public static class GoalExtensions
{
    public static string ToApiValue(this Goal goal) => goal switch
    {
        Goal.Strength => "strength",
        Goal.Hypertrophy => "hypertrophy",
        Goal.Endurance => "endurance",
        Goal.Flexibility => "flexibility",
        Goal.Cardio => "cardio",
        _ => throw new ArgumentOutOfRangeException(nameof(goal), goal, null)
    };

    public static Goal FromApiValue(string value) => value.ToLowerInvariant() switch
    {
        "strength" => Goal.Strength,
        "hypertrophy" => Goal.Hypertrophy,
        "endurance" => Goal.Endurance,
        "flexibility" => Goal.Flexibility,
        "cardio" => Goal.Cardio,
        _ => throw new ArgumentException($"Objetivo inválido: {value}", nameof(value))
    };
}
