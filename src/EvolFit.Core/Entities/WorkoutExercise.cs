namespace EvolFit.Core.Entities;

/// <summary>
/// Exercício dentro de uma rotina (linha por dia + exercício).
/// </summary>
public class WorkoutExercise
{
    public int Id { get; private set; }
    public int WorkoutRoutineId { get; private set; }
    public int DayNumber { get; private set; }
    public int WgerExerciseId { get; private set; }
    public string ExerciseName { get; private set; } = string.Empty;
    public int Sets { get; private set; } = 3;
    public int Reps { get; private set; } = 10;
    public decimal? Weight { get; private set; }
    public int? OrderInDay { get; private set; }

    // Navegação
    public WorkoutRoutine? WorkoutRoutine { get; private set; }
    public ICollection<ExerciseLog> Logs { get; private set; } = new List<ExerciseLog>();

    private WorkoutExercise() { }

    public static WorkoutExercise Create(
    int dayNumber,
    int wgerExerciseId,
    string exerciseName,
    int sets,
    int reps,
    int orderInDay)
    {
        if (dayNumber is < 1 or > 180)
            throw new ArgumentOutOfRangeException(nameof(dayNumber));

        if (sets is < 1 or > 20)
            throw new ArgumentOutOfRangeException(nameof(sets));

        if (reps is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(reps));

        if (string.IsNullOrWhiteSpace(exerciseName))
            throw new ArgumentException("Nome do exercício é obrigatório.", nameof(exerciseName));

        return new WorkoutExercise
        {
            DayNumber = dayNumber,
            WgerExerciseId = wgerExerciseId,
            ExerciseName = exerciseName.Trim(),
            Sets = sets,
            Reps = reps,
            OrderInDay = orderInDay
        };
    }

    public void SetWeight(decimal weight)
    {
        if (weight < 0)
            throw new ArgumentOutOfRangeException(nameof(weight));

        Weight = weight;
    }
}
