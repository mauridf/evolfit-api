namespace EvolFit.Core.Enums;

/// <summary>
/// Status de uma rotina de treinos.
/// Espelha o CHECK do banco: (0, 1, 2).
/// </summary>
public enum WorkoutStatus
{
    Paused = 0,
    Active = 1,
    Completed = 2
}
