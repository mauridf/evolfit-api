using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EvolFit.Application.Features.Wger.DTOs;
using EvolFit.Application.Features.Wger.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvolFit.Infrastructure.ExternalServices.Wger;

public class WgerExerciseClient : IWgerExerciseClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly WgerOptions _options;
    private readonly ILogger<WgerExerciseClient> _logger;

    public WgerExerciseClient(
        HttpClient httpClient,
        IOptions<WgerOptions> options,
        ILogger<WgerExerciseClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ExerciseSearchResponse> SearchExercisesAsync(string term, CancellationToken ct = default)
    {
        var lang = _options.Language == 2 ? "english" : "portuguese";
        var url = $"exercise/search/?term={Uri.EscapeDataString(term)}&language={lang}";

        var raw = await GetAsync<WgerSearchEnvelope>(url, ct);

        var results = raw?.Suggestions?
            .Select(s => new ExerciseSearchResultDto(
                s.Data?.Id ?? 0,
                s.Value ?? string.Empty,
                s.Data?.Description ?? string.Empty,
                s.Data?.Category ?? string.Empty,
                ParseMuscleString(s.Data?.Muscles)))
            .Where(r => r.Id > 0)
            .ToList() ?? new List<ExerciseSearchResultDto>();

        return new ExerciseSearchResponse(results);
    }

    public async Task<ExerciseListResponse> GetExercisesByMuscleAsync(
        int muscleId, CancellationToken ct = default) =>
        await GetExerciseListAsync(
            $"exerciseinfo/?muscles={muscleId}&language={_options.Language}&status=2", ct);

    public async Task<ExerciseListResponse> GetExercisesByCategoryAsync(
        int categoryId, CancellationToken ct = default) =>
        await GetExerciseListAsync(
            $"exerciseinfo/?category={categoryId}&language={_options.Language}&status=2", ct);

    public async Task<ExerciseListResponse> GetExercisesByEquipmentAsync(
        int equipmentId, CancellationToken ct = default) =>
        await GetExerciseListAsync(
            $"exerciseinfo/?equipment={equipmentId}&language={_options.Language}&status=2", ct);

    public async Task<ExerciseDetailResponse> GetExerciseInfoAsync(
        int exerciseId, CancellationToken ct = default)
    {
        var url = $"exerciseinfo/{exerciseId}/";
        var raw = await GetAsync<WgerExerciseInfoRaw>(url, ct)
            ?? throw new HttpRequestException($"Exercício {exerciseId} não encontrado na wger.");

        var item = ToListItem(raw);
        var equipment = raw.Equipment?.Select(e => e.Name ?? string.Empty).ToList()
            ?? new List<string>();
        var images = raw.Images?.Select(i => i.Image ?? string.Empty).ToList()
            ?? new List<string>();

        return new ExerciseDetailResponse(
            item.Id,
            item.Name,
            item.Description,
            item.Category,
            item.Muscles,
            equipment,
            images);
    }

    public async Task<MuscleListResponse> GetMusclesAsync(CancellationToken ct = default)
    {
        var raw = await GetAsync<WgerMuscleListRaw>("muscle/?limit=200", ct);

        var results = raw?.Results
            .Select(m => new MuscleDto(m.Id, m.Name ?? string.Empty, m.NameEn ?? string.Empty))
            .ToList() ?? new List<MuscleDto>();

        return new MuscleListResponse(results);
    }

    public async Task<CategoryListResponse> GetCategoriesAsync(CancellationToken ct = default)
    {
        var raw = await GetAsync<WgerCategoryListRaw>("exercisecategory/?limit=200", ct);

        var results = raw?.Results
            .Select(c => new CategoryDto(c.Id, c.Name ?? string.Empty))
            .ToList() ?? new List<CategoryDto>();

        return new CategoryListResponse(results);
    }

    private async Task<ExerciseListResponse> GetExerciseListAsync(
        string url, CancellationToken ct)
    {
        var raw = await GetAsync<WgerExerciseInfoListRaw>(url, ct);

        var results = raw?.Results?
            .Select(ToListItem)
            .Where(d => d.Id > 0)
            .ToList() ?? new List<ExerciseListItem>();

        return new ExerciseListResponse(results, results.Count);
    }

    // A lista retorna traduções por idioma; seleciona o idioma configurado (WGR-003).
    private ExerciseListItem ToListItem(WgerExerciseInfoRaw item)
    {
        var translation = item.Translations?
            .FirstOrDefault(t => t.Language == _options.Language)
            ?? item.Translations?.FirstOrDefault();

        var muscles = item.Muscles?.Select(m => m.Name ?? string.Empty).ToList()
            ?? new List<string>();

        return new ExerciseListItem(
            item.Id,
            translation?.Name ?? string.Empty,
            translation?.Description ?? string.Empty,
            item.Category?.Name ?? string.Empty,
            muscles);
    }

    private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken ct)
    {
        _logger.LogDebug("wger request: {Url}", relativeUrl);

        using var response = await _httpClient.GetAsync(relativeUrl, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("wger retornou {Status}: {Body}", response.StatusCode, body);
            throw new HttpRequestException($"wger API retornou {(int)response.StatusCode}");
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    private static List<string> ParseMuscleString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();
        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    // ---------- Envelopes específicos da wger ----------
    private sealed class WgerSearchEnvelope
    {
        [JsonPropertyName("suggestions")]
        public List<WgerSearchSuggestion>? Suggestions { get; set; }
    }

    private sealed class WgerSearchSuggestion
    {
        [JsonPropertyName("value")]
        public string? Value { get; set; }

        [JsonPropertyName("data")]
        public WgerSearchSuggestionData? Data { get; set; }
    }

    private sealed class WgerSearchSuggestionData
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("muscles")]
        public string? Muscles { get; set; }
    }

    private sealed class WgerExerciseInfoRaw
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("category")]
        public WgerCategoryRaw? Category { get; set; }

        [JsonPropertyName("muscles")]
        public List<WgerNameRaw>? Muscles { get; set; }

        [JsonPropertyName("equipment")]
        public List<WgerNameRaw>? Equipment { get; set; }

        [JsonPropertyName("images")]
        public List<WgerImageRaw>? Images { get; set; }

        [JsonPropertyName("translations")]
        public List<WgerTranslationRaw>? Translations { get; set; }
    }

    private sealed class WgerCategoryRaw
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class WgerNameRaw
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class WgerImageRaw
    {
        [JsonPropertyName("image")]
        public string? Image { get; set; }
    }

    private sealed class WgerTranslationRaw
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("language")]
        public int Language { get; set; }
    }

    private sealed class WgerExerciseInfoListRaw
    {
        [JsonPropertyName("results")]
        public List<WgerExerciseInfoRaw>? Results { get; set; }
    }

    private sealed class WgerMuscleRaw
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("name_en")]
        public string? NameEn { get; set; }
    }

    private sealed class WgerMuscleListRaw
    {
        [JsonPropertyName("results")]
        public List<WgerMuscleRaw> Results { get; set; } = new();
    }

    private sealed class WgerCategoryListRaw
    {
        [JsonPropertyName("results")]
        public List<WgerCategoryRaw> Results { get; set; } = new();
    }
}
