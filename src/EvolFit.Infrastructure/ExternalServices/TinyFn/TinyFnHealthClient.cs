using System.Net.Http.Json;
using System.Text.Json;
using EvolFit.Application.Features.TinyFn.DTOs;
using EvolFit.Application.Features.TinyFn.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvolFit.Infrastructure.ExternalServices.TinyFn;

public class TinyFnHealthClient : ITinyFnHealthClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly TinyFnOptions _options;
    private readonly ILogger<TinyFnHealthClient> _logger;

    public TinyFnHealthClient(
        HttpClient httpClient,
        IOptions<TinyFnOptions> options,
        ILogger<TinyFnHealthClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public Task<BmiResponse> CalculateBmiAsync(BmiRequest request, CancellationToken ct = default)
    {
        var url = $"bmi?weight_kg={request.WeightKg}&height_cm={request.HeightCm}";
        return GetAsync<BmiResponse>(url, ct);
    }

    public Task<BmrResponse> CalculateBmrAsync(BmrRequest request, CancellationToken ct = default)
    {
        var url = $"bmr?weight={request.Weight}&height={request.Height}&age={request.Age}&gender={request.Gender}";
        return GetAsync<BmrResponse>(url, ct);
    }

    public Task<TdeeResponse> CalculateTdeeAsync(TdeeRequest request, CancellationToken ct = default)
    {
        var url = $"tdee?weight={request.Weight}&height={request.Height}&age={request.Age}&gender={request.Gender}&activity={request.Activity}";
        return GetAsync<TdeeResponse>(url, ct);
    }

    public Task<CaloriesResponse> CalculateCaloriesAsync(CaloriesRequest request, CancellationToken ct = default)
    {
        var url = $"calories?weight={request.Weight}&height={request.Height}&age={request.Age}&gender={request.Gender}&activity={request.Activity}&goal={request.Goal}";
        return GetAsync<CaloriesResponse>(url, ct);
    }

    private async Task<T> GetAsync<T>(string relativeUrl, CancellationToken ct)
    {
        _logger.LogDebug("TinyFn request: {Url}", relativeUrl);

        using var response = await _httpClient.GetAsync(relativeUrl, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("TinyFn retornou {Status}: {Body}", response.StatusCode, body);
            throw new HttpRequestException(
                $"TinyFn Health API retornou {(int)response.StatusCode}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        if (result is null)
            throw new InvalidOperationException("TinyFn retornou resposta vazia.");

        return result;
    }
}
