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

    public async Task<IReadOnlyList<ExerciseListItemDto>> GetExercisesByMuscleAsync(
        int muscleId, CancellationToken ct = default)
    {
        var url = $"exercise/?muscles={muscleId}&language={_options.Language}&status=2";

        var raw = await GetAsync<WgerExerciseListRaw>(url, ct);

        return raw?.Results?
            .Select(r => new ExerciseListItemDto(
                r.Id,
                r.Name ?? string.Empty,
                r.Description ?? string.Empty,
                r.Category ?? string.Empty,
                r.Muscles ?? new List<string>(),
                r.Equipment ?? new List<string>()))
            .ToList() ?? new List<ExerciseListItemDto>();
    }

    public async Task<ExerciseDetailDto> GetExerciseInfoAsync(int exerciseId, CancellationToken ct = default)
    {
        var url = $"exerciseinfo/{exerciseId}/";
        var raw = await GetAsync<WgerExerciseInfoRaw>(url, ct)
            ?? throw new HttpRequestException($"Exercício {exerciseId} não encontrado na wger.");

        var muscles = raw.Muscles?.Select(m => m.Name ?? string.Empty).ToList() ?? new();
        var equipment = raw.Equipment?.Select(e => e.Name ?? string.Empty).ToList() ?? new();
        var images = raw.Images?.Select(i => i.Image ?? string.Empty).ToList() ?? new();

        return new ExerciseDetailDto(
            raw.Id,
            raw.Name ?? string.Empty,
            raw.Description ?? string.Empty,
            raw.Category?.Name ?? string.Empty,
            muscles,
            equipment,
            images);
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
}
