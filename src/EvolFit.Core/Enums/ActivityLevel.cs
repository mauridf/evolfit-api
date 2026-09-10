namespace EvolFit.Core.Enums;

/// <summary>
/// Nível de atividade física — multiplicador aplicado ao BMR para obter TDEE.
/// Valores de fator conforme Seção 17.2 do MASTER_SPECIFICATION.
/// </summary>
public enum ActivityLevel
{
    Sedentary = 1,  // 1.20 — pouco ou nenhum exercício
    Light = 2,  // 1.375 — leve 1-3 dias/semana
    Moderate = 3,  // 1.55 — moderado 3-5 dias/semana
    Active = 4,  // 1.725 — intenso 6-7 dias/semana
    Extreme = 5   // 1.90 — muito intenso / atleta
}

public static class ActivityLevelExtensions
{
    /// <summary>
    /// Retorna o multiplicador numérico do nível (usado em fallback local — TFN-005).
    /// </summary>
    public static double ToMultiplier(this ActivityLevel level) => level switch
    {
        ActivityLevel.Sedentary => 1.20,
        ActivityLevel.Light => 1.375,
        ActivityLevel.Moderate => 1.55,
        ActivityLevel.Active => 1.725,
        ActivityLevel.Extreme => 1.90,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

    /// <summary>
    /// Converte para o valor textual aceito pela TinyFn API (query string).
    /// </summary>
    public static string ToApiValue(this ActivityLevel level) => level switch
    {
        ActivityLevel.Sedentary => "sedentary",
        ActivityLevel.Light => "light",
        ActivityLevel.Moderate => "moderate",
        ActivityLevel.Active => "active",
        ActivityLevel.Extreme => "extreme",
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };

    /// <summary>
    /// Converte string (vinda do banco/API) para o enum.
    /// </summary>
    public static ActivityLevel FromApiValue(string value) => value.ToLowerInvariant() switch
    {
        "sedentary" => ActivityLevel.Sedentary,
        "light" => ActivityLevel.Light,
        "moderate" => ActivityLevel.Moderate,
        "active" => ActivityLevel.Active,
        "extreme" => ActivityLevel.Extreme,
        _ => throw new ArgumentException($"Nível de atividade inválido: {value}", nameof(value))
    };
}
