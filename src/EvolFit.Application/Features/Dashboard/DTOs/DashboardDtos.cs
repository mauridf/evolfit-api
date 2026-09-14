namespace EvolFit.Application.Features.Dashboard.DTOs;

// ---------- GET /dashboard ----------
public sealed record DashboardResponse(
    decimal? CurrentBmi,
    string? CurrentBmiCategory,
    int? CurrentTdee,
    int ActiveRoutines,
    int TotalHealthMetrics,
    int TodayExercises,
    int TodayCompleted,
    decimal TodayCompletionPercent,
    decimal WeeklyCompletionAvg);

// ---------- GET /dashboard/progress ----------
public sealed record DashboardProgressResponse(
    IReadOnlyList<string> Labels,
    IReadOnlyList<decimal> BmiData,
    IReadOnlyList<decimal> WeightData,
    decimal? TargetBmi);

// ---------- GET /dashboard/compliance ----------
public sealed record ComplianceDayDto(
    string Date,
    int TotalExercises,
    int Completed,
    decimal Percent);

public sealed record DashboardComplianceResponse(
    int Period,
    IReadOnlyList<ComplianceDayDto> Days,
    decimal AveragePercent);
