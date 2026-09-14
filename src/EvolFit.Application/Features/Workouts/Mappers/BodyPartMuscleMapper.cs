using EvolFit.Core.Enums;

namespace EvolFit.Application.Features.Workouts.Mappers;

/// <summary>
/// Mapeamento parte do corpo (input do usuário) → muscle ID (wger).
/// Conforme Seção 17.1 do MASTER_SPECIFICATION (WGR-003).
/// </summary>
public static class BodyPartMuscleMapper
{
    public static int ToMuscleId(BodyPart bodyPart) => bodyPart switch
    {
        BodyPart.Chest => 4,   // Pectoralis Major
        BodyPart.Back => 12,  // Latissimus Dorsi
        BodyPart.Legs => 10,  // Quadriceps
        BodyPart.Glutes => 8,   // Gluteus Maximus
        BodyPart.Shoulders => 13,  // Deltoideus
        BodyPart.Arms => 5,   // Biceps Brachii
        BodyPart.Abs => 11,  // Rectus Abdominis
        BodyPart.Cardio => 15,  // Cardio
        _ => throw new ArgumentOutOfRangeException(nameof(bodyPart), bodyPart, null)
    };

    public static IReadOnlyList<int> ToMuscleIds(IEnumerable<BodyPart> bodyParts) =>
        bodyParts.Select(ToMuscleId).Distinct().ToList();
}
