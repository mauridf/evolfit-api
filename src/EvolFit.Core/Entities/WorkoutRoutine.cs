using EvolFit.Core.Enums;

namespace EvolFit.Core.Entities;

/// <summary>
/// Rotina de treinos — agregado raiz do bounded context Workouts.
/// </summary>
public class WorkoutRoutine
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Goal? Goal { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public WorkoutStatus Status { get; private set; } = WorkoutStatus.Active;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navegação
    public User? User { get; private set; }
    public ICollection<WorkoutExercise> Exercises { get; private set; } = new List<WorkoutExercise>();

    private WorkoutRoutine() { }

    /// <summary>
    /// Cria uma nova rotina com período validado (RN-003: 7 a 180 dias).
    /// </summary>
    public static WorkoutRoutine Create(
        int userId,
        string name,
        Goal? goal,
        DateOnly startDate,
        int periodDays)
    {
        if (periodDays is < 7 or > 180)
            throw new ArgumentOutOfRangeException(nameof(periodDays),
                "Período da rotina deve estar entre 7 e 180 dias (VALID-002).");

        if (string.IsNullOrWhiteSpace(name) || name.Length is < 3 or > 255)
            throw new ArgumentException(
                "Nome da rotina deve ter entre 3 e 255 caracteres.", nameof(name));

        var endDate = startDate.AddDays(periodDays - 1);

        return new WorkoutRoutine
        {
            UserId = userId,
            Name = name.Trim(),
            Goal = goal,
            StartDate = startDate,
            EndDate = endDate,
            Status = WorkoutStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void ChangeStatus(WorkoutStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Total de dias úteis da rotina (inclusive).
    /// </summary>
    public int TotalDays => EndDate.DayNumber - StartDate.DayNumber + 1;
}
