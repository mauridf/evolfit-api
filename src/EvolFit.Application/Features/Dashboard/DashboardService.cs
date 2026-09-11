using EvolFit.Application.Common;
using EvolFit.Application.Features.Dashboard.DTOs;
using EvolFit.Application.Features.Health.Interfaces;
using EvolFit.Application.Features.Workouts.Interfaces;

namespace EvolFit.Application.Features.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IHealthMetricRepository _health;
    private readonly IWorkoutRoutineRepository _routines;
    private readonly IWorkoutExerciseRepository _exercises;
    private readonly IExerciseLogRepository _logs;
    private readonly ICurrentUserService _currentUser;

    public DashboardService(
        IHealthMetricRepository health,
        IWorkoutRoutineRepository routines,
        IWorkoutExerciseRepository exercises,
        IExerciseLogRepository logs,
        ICurrentUserService currentUser)
    {
        _health = health;
        _routines = routines;
        _exercises = exercises;
        _logs = logs;
        _currentUser = currentUser;
    }

    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // ---------- Saúde ----------
        var latest = await _health.GetLatestForUserAsync(userId, ct);
        var totalMetrics = await _health.CountForUserAsync(userId, ct);

        // ---------- Rotinas ----------
        var activeRoutine = await _routines.GetActiveForUserAsync(userId, ct);
        var activeRoutinesCount = activeRoutine is null ? 0 : 1;

        // ---------- Exercícios de hoje ----------
        int todayExercises = 0;
        int todayCompleted = 0;
        decimal todayPercent = 0m;

        if (activeRoutine is not null)
        {
            var dayNumber = today.DayNumber - activeRoutine.StartDate.DayNumber + 1;
            if (dayNumber >= 1 && dayNumber <= activeRoutine.TotalDays)
            {
                var dayExercises = await _exercises.GetByRoutineAndDayAsync(
                    activeRoutine.Id, dayNumber, ct);

                todayExercises = dayExercises.Count;

                if (todayExercises > 0)
                {
                    var logs = await _logs.GetByUserAndExerciseIdsAsync(
                        userId, dayExercises.Select(e => e.Id).ToList(), ct);

                    todayCompleted = logs
                        .Where(l => l.Date == today && l.Completed)
                        .Select(l => l.WorkoutExerciseId)
                        .Distinct()
                        .Count();

                    todayPercent = Math.Round(100m * todayCompleted / todayExercises, 2);
                }
            }
        }

        // ---------- Média semanal de conclusão ----------
        var weeklyAvg = await CalculateWeeklyAverageAsync(userId, ct);

        return new DashboardResponse(
            CurrentBmi: latest?.Bmi,
            CurrentBmiCategory: latest is null
                ? null
                : EvolFit.Application.Features.Health.FallbackHealthCalculator
                    .ClassifyBmi(latest.Bmi),
            CurrentTdee: latest?.Tdee,
            ActiveRoutines: activeRoutinesCount,
            TotalHealthMetrics: totalMetrics,
            TodayExercises: todayExercises,
            TodayCompleted: todayCompleted,
            TodayCompletionPercent: todayPercent,
            WeeklyCompletionAvg: weeklyAvg);
    }

    public async Task<DashboardProgressResponse> GetProgressAsync(
        int periodDays, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        periodDays = periodDays <= 0 ? 90 : Math.Clamp(periodDays, 1, 365);

        var metrics = await _health.GetEvolutionForUserAsync(userId, periodDays, ct);

        var labels = metrics
            .Select(m => m.MeasuredAt.ToString("yyyy-MM-dd"))
            .ToList();

        var bmiData = metrics.Select(m => m.Bmi).ToList();
        var weightData = metrics.Select(m => m.WeightKg).ToList();

        return new DashboardProgressResponse(labels, bmiData, weightData, TargetBmi: null);
    }

    public async Task<DashboardComplianceResponse> GetComplianceAsync(
        int days, CancellationToken ct = default)
    {
        var userId = _currentUser.UserId;
        days = days <= 0 ? 7 : Math.Clamp(days, 1, 90);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddDays(-(days - 1));

        var result = new List<ComplianceDayDto>();

        // Para cada dia, calcula total e concluídos
        for (var i = 0; i < days; i++)
        {
            var date = start.AddDays(i);
            var logs = await _logs.GetByUserAndDateAsync(userId, date, ct);

            var completedCount = logs.Count(l => l.Completed);
            var totalCount = logs.Count;

            var percent = totalCount == 0
                ? 0m
                : Math.Round(100m * completedCount / totalCount, 2);

            result.Add(new ComplianceDayDto(
                date.ToString("yyyy-MM-dd"),
                totalCount,
                completedCount,
                percent));
        }

        var daysWithActivity = result.Where(d => d.TotalExercises > 0).ToList();
        var avg = daysWithActivity.Count == 0
            ? 0m
            : Math.Round(daysWithActivity.Average(d => d.Percent), 2);

        return new DashboardComplianceResponse(days, result, avg);
    }

    // ---------- Helpers ----------
    private async Task<decimal> CalculateWeeklyAverageAsync(int userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalPercent = 0m;
        var daysWithData = 0;

        for (var i = 0; i < 7; i++)
        {
            var date = today.AddDays(-i);
            var logs = await _logs.GetByUserAndDateAsync(userId, date, ct);

            if (logs.Count == 0) continue;

            var completed = logs.Count(l => l.Completed);
            totalPercent += 100m * completed / logs.Count;
            daysWithData++;
        }

        return daysWithData == 0 ? 0m : Math.Round(totalPercent / daysWithData, 2);
    }
}
