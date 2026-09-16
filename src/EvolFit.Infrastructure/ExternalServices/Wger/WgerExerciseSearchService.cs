using System.Globalization;
using System.Text;
using System.Text.Json;
using EvolFit.Application.Common;
using EvolFit.Application.Features.Wger.DTOs;
using EvolFit.Application.Features.Wger.Interfaces;
using EvolFit.Core.Entities;
using Microsoft.Extensions.Logging;

namespace EvolFit.Infrastructure.ExternalServices.Wger;

/// <summary>
/// Busca de exercícios no catálogo local.
/// A wger não oferece busca textual pública; este serviço sincroniza o
/// catálogo completo (exerciseinfo paginado) para o cache local e faz a
/// busca via ILIKE, com expansão de termos PT→EN (ex.: "esteira"→"treadmill").
/// </summary>
public class WgerExerciseSearchService : IExerciseSearchService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromDays(7);

    // Expansão de termos em português para nomes em inglês usados pela wger
    // (a wger tem pouquíssimas traduções PT — 66 de 880 exercícios).
    private static readonly Dictionary<string, string[]> PtToEn =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["esteira"] = new[] { "treadmill" },
            ["corrida"] = new[] { "running", "run" },
            ["correr"] = new[] { "running", "run" },
            ["caminhada"] = new[] { "walking", "walk" },
            ["caminhar"] = new[] { "walking", "walk" },
            ["bicicleta"] = new[] { "bike", "cycling" },
            ["pedalar"] = new[] { "bike", "cycling" },
            ["bike"] = new[] { "bike", "cycling" },
            ["pular"] = new[] { "jump", "jumping" },
            ["pular corda"] = new[] { "jump rope" },
            ["corda"] = new[] { "jump rope" },
            ["agachamento"] = new[] { "squat" },
            ["agachar"] = new[] { "squat" },
            ["supino"] = new[] { "bench press", "bench" },
            ["rosca"] = new[] { "curl" },
            ["remada"] = new[] { "row", "rowing" },
            ["remador"] = new[] { "row", "rowing" },
            ["levantamento"] = new[] { "deadlift", "dead lift" },
            ["levantar"] = new[] { "deadlift", "lift" },
            ["prancha"] = new[] { "plank" },
            ["abdominal"] = new[] { "crunch", "abs", "abdominal" },
            ["abdomen"] = new[] { "crunch", "abs" },
            ["flexao"] = new[] { "push up", "pushup" },
            ["flexões"] = new[] { "push up", "pushup" },
            ["barra"] = new[] { "pull up", "chin up", "pullup" },
            ["ombro"] = new[] { "shoulder" },
            ["triceps"] = new[] { "triceps" },
            ["biceps"] = new[] { "biceps" },
            ["panturrilha"] = new[] { "calf" },
            ["alongamento"] = new[] { "stretch" },
            ["alongar"] = new[] { "stretch" },
            ["peitoral"] = new[] { "chest" },
            ["peito"] = new[] { "chest" },
            ["costas"] = new[] { "back" },
            ["perna"] = new[] { "leg" },
            ["pernas"] = new[] { "leg" },
            ["quadril"] = new[] { "hip" },
            ["gluteo"] = new[] { "glute" },
            ["gluteos"] = new[] { "glute" },
        };

    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private readonly IWgerExerciseClient _wger;
    private readonly IWgerExerciseCacheRepository _cache;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WgerExerciseSearchService> _logger;

    public WgerExerciseSearchService(
        IWgerExerciseClient wger,
        IWgerExerciseCacheRepository cache,
        IUnitOfWork uow,
        ILogger<WgerExerciseSearchService> logger)
    {
        _wger = wger;
        _cache = cache;
        _uow = uow;
        _logger = logger;
    }

    public async Task<ExerciseSearchResponse> SearchAsync(
        string term, string language, CancellationToken ct = default)
    {
        var trimmed = term.Trim();
        if (trimmed.Length == 0)
            return new ExerciseSearchResponse(new List<ExerciseSearchResultDto>());

        await EnsureCatalogAsync(ct);

        var mode = NormalizeLanguage(language);
        var terms = ExpandTerms(trimmed);

        var matches = await _cache.SearchByTermsAsync(terms, mode, limit: 100, ct);

        var results = matches
            .Select(e => new ExerciseSearchResultDto(
                e.WgerExerciseId,
                ResolveName(e, mode),
                e.Description ?? string.Empty,
                e.Category ?? string.Empty,
                DeserializeStrings(e.MusclesJson)))
            .Where(r => r.Id > 0)
            .ToList();

        return new ExerciseSearchResponse(results);
    }

    private async Task EnsureCatalogAsync(CancellationToken ct)
    {
        var maxExpires = await _cache.MaxCatalogExpiresAtAsync(ct);
        if (maxExpires.HasValue && maxExpires.Value > DateTime.UtcNow)
            return;

        await SyncLock.WaitAsync(ct);
        try
        {
            // re-verifica após adquirir o lock (outra request pode ter sincronizado)
            maxExpires = await _cache.MaxCatalogExpiresAtAsync(ct);
            if (maxExpires.HasValue && maxExpires.Value > DateTime.UtcNow)
                return;

            var catalog = await _wger.GetCatalogAsync(ct);

            await _cache.RemoveCatalogEntriesAsync(ct);
            foreach (var item in catalog)
            {
                await _cache.AddAsync(
                    WgerExerciseCache.Create(
                        item.Id,
                        item.NameEn,
                        item.DescriptionEn,
                        item.Category,
                        JsonSerializer.Serialize(item.Muscles),
                        item.Equipment is { Count: > 0 }
                            ? JsonSerializer.Serialize(item.Equipment)
                            : null,
                        item.Images is { Count: > 0 }
                            ? JsonSerializer.Serialize(item.Images)
                            : null,
                        CacheTtl,
                        item.MuscleId,
                        item.CategoryId,
                        item.NamePt),
                    ct);
            }

            await _uow.SaveChangesAsync(ct);
            _logger.LogInformation("Catálogo wger sincronizado: {Count} exercícios", catalog.Count);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private static string NormalizeLanguage(string language) => language.ToLowerInvariant() switch
    {
        "english" => "english",
        "portuguese" => "portuguese",
        _ => "all"
    };

    private static string ResolveName(WgerExerciseCache entry, string mode) => mode switch
    {
        "english" => entry.Name,
        "portuguese" => entry.NamePt ?? entry.Name,
        _ => entry.NamePt ?? entry.Name
    };

    private static IReadOnlyCollection<string> ExpandTerms(string term)
    {
        var normalized = RemoveDiacritics(term.ToLowerInvariant().Trim());
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { term.Trim() };

        if (PtToEn.TryGetValue(normalized, out var fullExpansions))
        {
            foreach (var expansion in fullExpansions)
                terms.Add(expansion);
        }

        foreach (var word in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (PtToEn.TryGetValue(word, out var wordExpansions))
            {
                foreach (var expansion in wordExpansions)
                    terms.Add(expansion);
            }
            else
            {
                terms.Add(word);
            }
        }

        return terms;
    }

    public static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static List<string> DeserializeStrings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}