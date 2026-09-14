using EvolFit.Core.Entities;
using EvolFit.Core.Enums;
using FluentAssertions;

namespace EvolFit.UnitTests.Core;

public class HealthMetricTests
{
    [Fact]
    public void Create_WithValidValues_ShouldSucceed()
    {
        var metric = HealthMetric.Create(
            userId: 1,
            heightCm: 180m,
            weightKg: 75.5m,
            bmi: 23.30m,
            bmr: 1680,
            tdee: 2604,
            activityLevel: ActivityLevel.Moderate);

        metric.UserId.Should().Be(1);
        metric.Bmi.Should().Be(23.30m);
        metric.ActivityLevel.Should().Be(ActivityLevel.Moderate);
        metric.MeasuredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(99, 75)]      // altura baixa
    [InlineData(251, 75)]     // altura alta
    [InlineData(180, 39)]     // peso baixo
    [InlineData(180, 301)]    // peso alto
    public void Create_WithOutOfRangeValues_ShouldThrow(decimal height, decimal weight)
    {
        var act = () => HealthMetric.Create(1, height, weight, 20, 1500, 2000, ActivityLevel.Sedentary);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
