namespace EvolFit.Core.Enums;

/// <summary>
/// Nível de dificuldade da rotina.
/// </summary>
public enum Difficulty
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3
}

public static class DifficultyExtensions
{
    public static string ToApiValue(this Difficulty d) => d switch
    {
        Difficulty.Beginner => "beginner",
        Difficulty.Intermediate => "intermediate",
        Difficulty.Advanced => "advanced",
        _ => throw new ArgumentOutOfRangeException(nameof(d), d, null)
    };

    public static Difficulty FromApiValue(string value) => value.ToLowerInvariant() switch
    {
        "beginner" => Difficulty.Beginner,
        "intermediate" => Difficulty.Intermediate,
        "advanced" => Difficulty.Advanced,
        _ => throw new ArgumentException($"Dificuldade inválida: {value}", nameof(value))
    };
}
