namespace EvolFit.Application.Features.Wger.DTOs;

// ---------- Internos (chamada HTTP bruta) ----------
public sealed record WgerExerciseSearchResult(
    int Id, string Name, string Description, string Category, string Muscles);

public sealed record WgerExerciseSearchRaw(List<WgerExerciseSearchResult> Results);

public sealed record WgerExerciseDetailRaw(
    int Id, string Name, string Description, string Category,
    List<string> Muscles, List<string> Equipment, List<string> Images);

// ---------- Públicos (retorno da API EvolFit) ----------
public sealed record ExerciseSearchResultDto(
    int Id, string Name, string Description, string Category, List<string> Muscles);

public sealed record ExerciseSearchResponse(IReadOnlyList<ExerciseSearchResultDto> Results);

public sealed record ExerciseDetailDto(
    int Id, string Name, string Description, string Category,
    List<string> Muscles, List<string> Equipment, List<string> Images);

public sealed record ExerciseListItemDto(
    int Id, string Name, string Description, string Category,
    List<string> Muscles, List<string> Equipment);
