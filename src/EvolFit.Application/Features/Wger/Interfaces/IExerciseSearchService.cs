using EvolFit.Application.Features.Wger.DTOs;

namespace EvolFit.Application.Features.Wger.Interfaces;

public interface IExerciseSearchService
{
    /// <summary>
    /// Busca exercícios no catálogo local (sincronizado da wger).
    /// language: "english", "portuguese" ou "all" (padrão).
    /// </summary>
    Task<ExerciseSearchResponse> SearchAsync(
        string term, string language, CancellationToken ct = default);
}