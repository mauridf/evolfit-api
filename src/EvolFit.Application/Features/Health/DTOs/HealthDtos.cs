namespace EvolFit.Application.Features.Health.DTOs;

// ---------- Requests ----------
public sealed record CreateHealthMetricRequest(
    decimal WeightKg,
    decimal HeightCm,
    string Gender,
    int Age,
    string ActivityLevel);

// ---------- Responses ----------
public sealed record HealthMetricResponse(
    int Id,
    decimal HeightCm,
    decimal WeightKg,
    decimal Bmi,
    int? Bmr,
    int? Tdee,
    string? ActivityLevel,
    DateTime MeasuredAt,
    MacrosDto? MacrosSuggestion);

public sealed record MacrosDto(int ProteinG, int CarbsG, int FatG);

public sealed record HealthMetricListItem(
    int Id,
    decimal WeightKg,
    decimal HeightCm,
    decimal Bmi,
    int? Bmr,
    int? Tdee,
    string? ActivityLevel,
    DateTime MeasuredAt);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record EvolutionDataPoint(string Date, decimal Bmi, decimal WeightKg);

public sealed record EvolutionResponse(
    IReadOnlyList<EvolutionDataPoint> Data,
    decimal? StartBmi,
    decimal CurrentBmi,
    decimal? BmiChange,
    decimal? StartWeight,
    decimal CurrentWeight,
    decimal? WeightChange);
