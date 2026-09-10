namespace EvolFit.Core.ValueObjects;

/// <summary>
/// Value Object de IMC. Armazena o valor e a categoria calculada
/// (a categoria final vem da TinyFn, mas temos fallback local — TFN-005).
/// </summary>
public sealed record Bmi
{
    public decimal Value { get; }
    public string Category { get; }

    private Bmi(decimal value, string category)
    {
        Value = value;
        Category = category;
    }

    /// <summary>
    /// Cria um Bmi já com o valor e a categoria calculados (vindos da TinyFn).
    /// </summary>
    public static Bmi FromCalculated(decimal value, string category)
    {
        if (value <= 0 || value > 100)
            throw new ArgumentOutOfRangeException(nameof(value),
                "IMC deve estar entre 0 e 100.");

        return new Bmi(Math.Round(value, 2), category);
    }

    /// <summary>
    /// Fallback local — cálculo aproximado (TFN-005), usado apenas quando
    /// a TinyFn está indisponível. A categoria segue a classificação da OMS.
    /// </summary>
    public static Bmi CalculateFallback(double weightKg, double heightCm)
    {
        if (heightCm <= 0)
            throw new ArgumentOutOfRangeException(nameof(heightCm));

        var heightM = heightCm / 100.0;
        var bmi = weightKg / (heightM * heightM);
        var rounded = (decimal)Math.Round(bmi, 2);

        var category = rounded switch
        {
            < 18.5m => "Underweight",
            < 25.0m => "Normal weight",
            < 30.0m => "Overweight",
            < 35.0m => "Obesity class I",
            < 40.0m => "Obesity class II",
            _ => "Obesity class III"
        };

        return new Bmi(rounded, category);
    }
}
