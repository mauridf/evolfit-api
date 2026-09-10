namespace EvolFit.Core.Enums;

/// <summary>
/// Partes do corpo selecionáveis pelo usuário ao gerar uma rotina.
/// O mapeamento parte do corpo → muscle ID (wger) fica em Workouts (Application).
/// </summary>
public enum BodyPart
{
    Chest = 1,
    Back = 2,
    Legs = 3,
    Glutes = 4,
    Shoulders = 5,
    Arms = 6,
    Abs = 7,
    Cardio = 8
}

public static class BodyPartExtensions
{
    public static string ToApiValue(this BodyPart part) => part switch
    {
        BodyPart.Chest => "chest",
        BodyPart.Back => "back",
        BodyPart.Legs => "legs",
        BodyPart.Glutes => "glutes",
        BodyPart.Shoulders => "shoulders",
        BodyPart.Arms => "arms",
        BodyPart.Abs => "abs",
        BodyPart.Cardio => "cardio",
        _ => throw new ArgumentOutOfRangeException(nameof(part), part, null)
    };

    public static BodyPart FromApiValue(string value) => value.ToLowerInvariant() switch
    {
        "chest" => BodyPart.Chest,
        "back" => BodyPart.Back,
        "legs" => BodyPart.Legs,
        "glutes" => BodyPart.Glutes,
        "shoulders" => BodyPart.Shoulders,
        "arms" => BodyPart.Arms,
        "abs" => BodyPart.Abs,
        "cardio" => BodyPart.Cardio,
        _ => throw new ArgumentException($"Parte do corpo inválida: {value}", nameof(value))
    };
}
