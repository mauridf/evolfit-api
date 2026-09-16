using EvolFit.Core.Enums;

namespace EvolFit.Application.Features.Workouts.Mappers;

/// <summary>
/// Mapeamento parte do corpo (input do usuário) → referência wger.
/// IDs de músculo conforme GET /api/v2/muscle/ (CONF-002) e Seção 17.1
/// do MASTER_SPECIFICATION (WGR-003). Cardio não tem "músculo" na wger —
/// é a categoria de exercício 15 (WGR-003).
/// </summary>
public static class BodyPartMuscleMapper
{
    /// <summary>ID da categoria "Cardio" em GET /api/v2/exercisecategory/.</summary>
    public const int CardioCategoryId = 15;

    public static int ToMuscleId(BodyPart bodyPart) => bodyPart switch
    {
        BodyPart.Chest => 4,   // Pectoralis Major
        BodyPart.Back => 12,   // Latissimus Dorsi
        BodyPart.Legs => 10,   // Quadriceps Femoris
        BodyPart.Glutes => 8,  // Gluteus Maximus
        BodyPart.Shoulders => 2,  // Anterior Deltoid
        BodyPart.Arms => 1,    // Biceps Brachii
        BodyPart.Abs => 6,     // Rectus Abdominis
        BodyPart.Cardio => throw new ArgumentOutOfRangeException(
            nameof(bodyPart), bodyPart, "Cardio é resolvido por categoria, não por músculo."),
        _ => throw new ArgumentOutOfRangeException(nameof(bodyPart), bodyPart, null)
    };

    public static IReadOnlyList<int> ToMuscleIds(IEnumerable<BodyPart> bodyParts) =>
        bodyParts.Where(p => p != BodyPart.Cardio)
            .Select(ToMuscleId)
            .Distinct()
            .ToList();
}