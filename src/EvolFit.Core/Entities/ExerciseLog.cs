namespace EvolFit.Core.Entities;

/// <summary>
/// Registro de conclusão (ou não) de um exercício em uma data.
/// </summary>
public class ExerciseLog
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int WorkoutExerciseId { get; private set; }
    public DateOnly Date { get; private set; }
    public bool Completed { get; private set; }

    // Navegação
    public User? User { get; private set; }
    public WorkoutExercise? WorkoutExercise { get; private set; }

    private ExerciseLog() { }

    public static ExerciseLog Create(
        int userId,
        int workoutExerciseId,
        DateOnly date,
        bool completed)
    {
        return new ExerciseLog
        {
            UserId = userId,
            WorkoutExerciseId = workoutExerciseId,
            Date = date,
            Completed = completed
        };
    }

    public void MarkCompleted(bool completed)
    {
        Completed = completed;
    }
}
