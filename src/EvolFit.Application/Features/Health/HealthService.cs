using EvolFit.Application.Common;
using EvolFit.Application.Common.Exceptions;
using EvolFit.Application.Features.Health.DTOs;
using EvolFit.Application.Features.Health.Interfaces;
using EvolFit.Application.Features.TinyFn.DTOs;
using EvolFit.Application.Features.TinyFn.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Core.Enums;
using Microsoft.Extensions.Logging;

namespace EvolFit.Application.Features.Health;

public interface IHealthService
{
    Task<HealthMetricResponse> CreateAsync(CreateHealthMetricRequest request, CancellationToken ct = default);
    Task<PagedResponse<HealthMetricListItem>> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task<HealthMetricResponse> GetLatestAsync(CancellationToken ct = default);
    Task<HealthMetricResponse> GetByIdAsync(int id, CancellationToken ct = default);
    Task<EvolutionResponse> GetEvolutionAsync(int periodDays, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public class HealthService : IHealthService
{
    private readonly IHealthMetricRepository _repo;
    private readonly ITinyFnHealthClient _tinyFn;
    private readonly ITinyFnCache _cache;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<HealthService> _logger;

    public HealthService(
        IHealthMetricRepository repo,
        ITinyFnHealthClient tinyFn,
        ITinyFnCache cache,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        ILogger<HealthService> logger)
    {
        _repo = repo;
        _tinyFn = tinyFn;
        _cache = cache;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<HealthMetricResponse> CreateAsync(
        CreateHealthMetricRequest request,
        CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var gender = ParseGender(request.Gender);
        var activity = ActivityLevelExtensions.FromApiValue(request.ActivityLevel);

        // Chave de cache por parâmetros idênticos (TFN-003)
        var cacheKey = BuildCacheKey(request);

        var result = await _cache.GetOrCreateAsync(
            cacheKey,
            factory: () => CalculateViaTinyFnAsync(request, gender, activity, ct),
            ttl: TimeSpan.FromHours(24),
            ct: ct);

        var metric = HealthMetric.Create(
            userId: userId,
            heightCm: request.HeightCm,
            weightKg: request.WeightKg,
            bmi: result.Bmi,
            bmr: result.Bmr,
            tdee: result.Tdee,
            activityLevel: activity);

        await _repo.AddAsync(metric, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Nova medição registrada: UserId={UserId}, MetricId={Id}, Bmi={Bmi}",
            userId, metric.Id, metric.Bmi);

        return MapToResponse(metric, result.Macros);
    }

    public async Task<PagedResponse<HealthMetricListItem>> ListAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _repo.GetPagedForUserAsync(_currentUser.UserId, page, pageSize, ct);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return new PagedResponse<HealthMetricListItem>(
            items.Select(MapToListItem).ToList(),
            page,
            pageSize,
            total,
            totalPages);
    }

    public async Task<HealthMetricResponse> GetLatestAsync(CancellationToken ct = default)
    {
        var metric = await _repo.GetLatestForUserAsync(_currentUser.UserId, ct)
            ?? throw new NotFoundException("Nenhuma medição de saúde encontrada.");

        return MapToResponse(metric, null);
    }

    public async Task<HealthMetricResponse> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var metric = await _repo.GetByIdForUserAsync(id, _currentUser.UserId, ct)
            ?? throw new NotFoundException("Medição de saúde não encontrada.");

        return MapToResponse(metric, null);
    }

    public async Task<EvolutionResponse> GetEvolutionAsync(int periodDays, CancellationToken ct = default)
    {
        periodDays = periodDays <= 0 ? 90 : Math.Clamp(periodDays, 1, 365);

        var metrics = await _repo.GetEvolutionForUserAsync(_currentUser.UserId, periodDays, ct);

        if (metrics.Count == 0)
            throw new NotFoundException("Nenhuma medição encontrada no período.");

        var data = metrics
            .Select(m => new EvolutionDataPoint(
                m.MeasuredAt.ToString("yyyy-MM-dd"),
                m.Bmi,
                m.WeightKg))
            .ToList();

        var first = metrics.First();
        var last = metrics.Last();

        return new EvolutionResponse(
            data,
            StartBmi: first.Bmi,
            CurrentBmi: last.Bmi,
            BmiChange: last.Bmi - first.Bmi,
            StartWeight: first.WeightKg,
            CurrentWeight: last.WeightKg,
            WeightChange: last.WeightKg - first.WeightKg);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var metric = await _repo.GetByIdForUserAsync(id, _currentUser.UserId, ct)
            ?? throw new NotFoundException("Medição de saúde não encontrada.");

        _repo.Delete(metric);
        await _uow.SaveChangesAsync(ct);
    }

    // ---------- Helpers ----------
    private async Task<(decimal Bmi, int? Bmr, int? Tdee, MacrosDto? Macros)> CalculateViaTinyFnAsync(
        CreateHealthMetricRequest req, Gender gender, ActivityLevel activity, CancellationToken ct)
    {
        try
        {
            var bmiTask = _tinyFn.CalculateBmiAsync(
                new BmiRequest((double)req.WeightKg, (double)req.HeightCm), ct);

            var tdeeTask = _tinyFn.CalculateTdeeAsync(
                new TdeeRequest(
                    (double)req.WeightKg,
                    (double)req.HeightCm,
                    req.Age,
                    gender.ToString().ToLowerInvariant(),
                    activity.ToApiValue()),
                ct);

            await Task.WhenAll(bmiTask, tdeeTask);

            var bmi = await bmiTask;
            var tdee = await tdeeTask;

            var macros = tdee.MacrosSuggestion is null
                ? null
                : new MacrosDto(
                    tdee.MacrosSuggestion.ProteinG,
                    tdee.MacrosSuggestion.CarbsG,
                    tdee.MacrosSuggestion.FatG);

            return (bmi.Bmi, tdee.Bmr, tdee.Tdee, macros);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "TinyFn indisponível — usando fallback local (TFN-005) para UserId={UserId}",
                _currentUser.UserId);

            var bmiLocal = FallbackHealthCalculator.CalculateBmi(
                (double)req.WeightKg, (double)req.HeightCm);

            var bmrLocal = FallbackHealthCalculator.CalculateBmr(
                (double)req.WeightKg, (double)req.HeightCm, req.Age, gender);

            var tdeeLocal = FallbackHealthCalculator.CalculateTdee(bmrLocal, activity);
            var (p, c, f) = FallbackHealthCalculator.SuggestMacros(tdeeLocal);

            return (bmiLocal, bmrLocal, tdeeLocal, new MacrosDto(p, c, f));
        }
    }

    private static string BuildCacheKey(CreateHealthMetricRequest req) =>
        $"tinyfn:calc:w{req.WeightKg}:h{req.HeightCm}:a{req.Age}:g{req.Gender}:l{req.ActivityLevel}".ToLowerInvariant();

    private static Gender ParseGender(string gender) =>
        gender.ToLowerInvariant() == "male" ? Gender.Male : Gender.Female;

    private static HealthMetricResponse MapToResponse(HealthMetric m, MacrosDto? macros) =>
        new(
            m.Id,
            m.HeightCm,
            m.WeightKg,
            m.Bmi,
            m.Bmr,
            m.Tdee,
            m.ActivityLevel?.ToApiValue(),
            m.MeasuredAt,
            macros);

    private static HealthMetricListItem MapToListItem(HealthMetric m) =>
        new(
            m.Id,
            m.WeightKg,
            m.HeightCm,
            m.Bmi,
            m.Bmr,
            m.Tdee,
            m.ActivityLevel?.ToApiValue(),
            m.MeasuredAt);
}
