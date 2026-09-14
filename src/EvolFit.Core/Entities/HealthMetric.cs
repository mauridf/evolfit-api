using EvolFit.Core.Enums;

namespace EvolFit.Core.Entities;

/// <summary>
/// Medição de saúde — agregado raiz do bounded context Health.
/// IMC/BMR/TDEE são calculados via TinyFn e persistidos (DAT-003).
/// </summary>
public class HealthMetric
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public decimal HeightCm { get; private set; }
    public decimal WeightKg { get; private set; }
    public decimal Bmi { get; private set; }
    public int? Bmr { get; private set; }
    public int? Tdee { get; private set; }
    public ActivityLevel? ActivityLevel { get; private set; }
    public int? ProteinG { get; private set; }
    public int? CarbsG { get; private set; }
    public int? FatG { get; private set; }
    public DateTime MeasuredAt { get; private set; }

    // Navegação
    public User? User { get; private set; }

    private HealthMetric() { }

    /// <summary>
    /// Cria uma medição já com todos os valores calculados pela TinyFn
    /// (ou fallback local — TFN-005).
    /// </summary>
    public static HealthMetric Create(
        int userId,
        decimal heightCm,
        decimal weightKg,
        decimal bmi,
        int? bmr,
        int? tdee,
        ActivityLevel? activityLevel,
        int? proteinG = null,
        int? carbsG = null,
        int? fatG = null)
    {
        if (heightCm is < 100 or > 250)
            throw new ArgumentOutOfRangeException(nameof(heightCm),
                "Altura deve estar entre 100 e 250 cm (VALID-001).");

        if (weightKg is < 40 or > 300)
            throw new ArgumentOutOfRangeException(nameof(weightKg),
                "Peso deve estar entre 40 e 300 kg (VALID-001).");

        return new HealthMetric
        {
            UserId = userId,
            HeightCm = heightCm,
            WeightKg = weightKg,
            Bmi = bmi,
            Bmr = bmr,
            Tdee = tdee,
            ActivityLevel = activityLevel,
            ProteinG = proteinG,
            CarbsG = carbsG,
            FatG = fatG,
            MeasuredAt = DateTime.UtcNow
        };
    }
}
