using EvolFit.Application.Features.Health;
using EvolFit.Core.Enums;
using FluentAssertions;

namespace EvolFit.UnitTests.Core;

public class FallbackHealthCalculatorTests
{
    [Theory]
    [InlineData(75.5, 180.0, 23.30)]
    [InlineData(60.0, 170.0, 20.76)]
    [InlineData(95.0, 175.0, 31.02)]
    public void CalculateBmi_ShouldMatchExpected(double weight, double height, decimal expected)
    {
        var result = FallbackHealthCalculator.CalculateBmi(weight, height);
        result.Should().BeApproximately(expected, 0.05m);
    }

    [Theory]
    [InlineData(17.0, "Underweight")]
    [InlineData(22.0, "Normal weight")]
    [InlineData(27.0, "Overweight")]
    [InlineData(32.0, "Obesity class I")]
    [InlineData(37.0, "Obesity class II")]
    [InlineData(45.0, "Obesity class III")]
    public void ClassifyBmi_ShouldReturnExpectedCategory(decimal bmi, string expected) =>
        FallbackHealthCalculator.ClassifyBmi(bmi).Should().Be(expected);

    [Fact]
    public void CalculateBmr_Male_ShouldUseMifflinStJeorFormula()
    {
        // 10*75.5 + 6.25*180 - 5*28 + 5 = 1680 (arredondado)
        var bmr = FallbackHealthCalculator.CalculateBmr(75.5, 180, 28, Gender.Male);
        bmr.Should().Be(1680);
    }

    [Fact]
    public void CalculateBmr_Female_ShouldSubtract161()
    {
        // 10*60 + 6.25*165 - 5*30 - 161 = 1330 (arredondado)
        var bmr = FallbackHealthCalculator.CalculateBmr(60, 165, 30, Gender.Female);
        bmr.Should().Be(1330);
    }

    [Theory]
    [InlineData(ActivityLevel.Sedentary, 2016)] // 1680 * 1.20
    [InlineData(ActivityLevel.Moderate, 2604)]  // 1680 * 1.55
    [InlineData(ActivityLevel.Extreme, 3192)]   // 1680 * 1.90
    public void CalculateTdee_ShouldApplyMultiplier(ActivityLevel level, int expected)
    {
        var tdee = FallbackHealthCalculator.CalculateTdee(1680, level);
        tdee.Should().Be(expected);
    }

    [Fact]
    public void SuggestMacros_ShouldSumApproximatelyToTdee()
    {
        var (p, c, f) = FallbackHealthCalculator.SuggestMacros(2604);
        var kcal = p * 4 + c * 4 + f * 9;
        kcal.Should().BeInRange(2550, 2660);
    }
}
