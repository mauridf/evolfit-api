namespace EvolFit.Application.Features.Wger.DTOs;

// Contratos conforme MASTER_SPECIFICATION §16.3 (WGR).

public sealed record ExerciseSearchResultDto(
    int Id, string Name, string Description, string Category, List<string> Muscles);

public sealed record ExerciseSearchResponse(IReadOnlyList<ExerciseSearchResultDto> Results);

public sealed record ExerciseListItem(
    int Id, string Name, string Description, string Category, List<string> Muscles);

public sealed record ExerciseListResponse(List<ExerciseListItem> Results, int Count);

public sealed record ExerciseDetailResponse(
    int Id, string Name, string Description, string Category,
    List<string> Muscles, List<string> Equipment, List<string> Images);

public sealed record MuscleDto(int Id, string Name, string NameEn);
public sealed record MuscleListResponse(List<MuscleDto> Results);

public sealed record CategoryDto(int Id, string Name);
public sealed record CategoryListResponse(List<CategoryDto> Results);