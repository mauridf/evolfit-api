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

    private const int PageSize = 100;
    private const int WgerLanguagePt = 7;
    private const int WgerLanguageEn = 2;

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

    public async Task<IReadOnlyList<ExerciseCatalogItem>> GetCatalogAsync(
        CancellationToken ct = default)
    {
        var results = new List<ExerciseCatalogItem>();
        var offset = 0;

        while (true)
        {
            var url = $"exerciseinfo/?status=2&limit={PageSize}&offset={offset}";
            var raw = await GetAsync<WgerExerciseInfoListRaw>(url, ct);

            if (raw?.Results is null || raw.Results.Count == 0)
                break;

            foreach (var item in raw.Results)
            {
                var translationEn = item.Translations?
                    .FirstOrDefault(t => t.Language == WgerLanguageEn);
                var translationPt = item.Translations?
                    .FirstOrDefault(t => t.Language == WgerLanguagePt);

                var muscles = item.Muscles?.Select(m => m.Name ?? string.Empty).ToList()
                    ?? new List<string>();
                var equipment = item.Equipment?.Select(e => e.Name ?? string.Empty).ToList()
                    ?? new List<string>();
                var images = item.Images?.Select(i => i.Image ?? string.Empty).ToList()
                    ?? new List<string>();

                results.Add(new ExerciseCatalogItem(
                    item.Id,
                    translationEn?.Name ?? string.Empty,
                    translationPt?.Name,
                    translationEn?.Description,
                    translationPt?.Description,
                    item.Category?.Name,
                    item.Category?.Id,
                    item.Muscles?.FirstOrDefault()?.Id,
                    muscles,
                    equipment,
                    images));
            }

            if (raw.Results.Count < PageSize)
                break;

            offset += PageSize;
        }

        return results;
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
