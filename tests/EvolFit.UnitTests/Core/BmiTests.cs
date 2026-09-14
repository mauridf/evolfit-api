using EvolFit.Core.ValueObjects;
using FluentAssertions;

namespace EvolFit.UnitTests.Core;

public class BmiTests
{
    [Fact]
    public void FromCalculated_WithValidValues_ShouldRoundToTwoDecimals()
    {
        var bmi = Bmi.FromCalculated(23.305m, "Normal weight");
        bmi.Value.Should().Be(23.31m);
        bmi.Category.Should().Be("Normal weight");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void FromCalculated_WithInvalidValues_ShouldThrow(decimal invalid)
    {
        var act = () => Bmi.FromCalculated(invalid, "x");
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CalculateFallback_ShouldClassifyNormalWeight()
    {
        var bmi = Bmi.CalculateFallback(75.5, 180);
        bmi.Category.Should().Be("Normal weight");
    }
}
