using EvolFit.Core.Enums;

namespace EvolFit.Application.Features.Health;

/// <summary>
/// Cálculos locais aproximados usados quando a TinyFn está indisponível (TFN-005).
/// Fórmula BMR: Mifflin-St Jeor. TDEE: BMR × multiplicador de atividade.
/// </summary>
public static class FallbackHealthCalculator
{
    public static decimal CalculateBmi(double weightKg, double heightCm)
    {
        var heightM = heightCm / 100.0;
        var bmi = weightKg / (heightM * heightM);
        return (decimal)Math.Round(bmi, 2);
    }

    public static string ClassifyBmi(decimal bmi) => bmi switch
    {
        < 18.5m => "Underweight",
        < 25.0m => "Normal weight",
        < 30.0m => "Overweight",
        < 35.0m => "Obesity class I",
        < 40.0m => "Obesity class II",
        _ => "Obesity class III"
    };

    /// <summary>
    /// BMR pela fórmula Mifflin-St Jeor.
    /// Homem: 10*peso + 6.25*altura − 5*idade + 5
    /// Mulher: 10*peso + 6.25*altura − 5*idade − 161
    /// </summary>
    public static int CalculateBmr(double weightKg, double heightCm, int age, Gender gender)
    {
        var baseCalc = (10 * weightKg) + (6.25 * heightCm) - (5 * age);
        var bmr = gender == Gender.Male ? baseCalc + 5 : baseCalc - 161;
        return (int)Math.Round(bmr);
    }

    public static int CalculateTdee(int bmr, ActivityLevel activity) =>
        (int)Math.Round(bmr * activity.ToMultiplier());

    public static (int Protein, int Carbs, int Fat) SuggestMacros(int tdee)
    {
        // Distribuição padrão ~30/40/30 (proteína/carbo/gordura)
        var protein = (int)Math.Round(tdee * 0.30 / 4.0);
        var carbs = (int)Math.Round(tdee * 0.40 / 4.0);
        var fat = (int)Math.Round(tdee * 0.30 / 9.0);
        return (protein, carbs, fat);
    }
}
