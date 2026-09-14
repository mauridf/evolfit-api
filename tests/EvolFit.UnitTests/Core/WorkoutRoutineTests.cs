using EvolFit.Core.Entities;
using EvolFit.Core.Enums;
using FluentAssertions;

namespace EvolFit.UnitTests.Core;

public class WorkoutRoutineTests
{
    [Fact]
    public void Create_With30Days_ShouldComputeEndDate()
    {
        var start = new DateOnly(2026, 9, 11);
        var routine = WorkoutRoutine.Create(1, "Treino", Goal.Hypertrophy, start, 30);

        routine.StartDate.Should().Be(start);
        routine.EndDate.Should().Be(start.AddDays(29));
        routine.TotalDays.Should().Be(30);
        routine.Status.Should().Be(WorkoutStatus.Active);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(181)]
    public void Create_WithInvalidPeriod_ShouldThrow(int days)
    {
        var act = () => WorkoutRoutine.Create(1, "Treino", null, DateOnly.FromDateTime(DateTime.UtcNow), days);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Create_WithInvalidName_ShouldThrow(string name)
    {
        var act = () => WorkoutRoutine.Create(1, name, null, DateOnly.FromDateTime(DateTime.UtcNow), 30);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ChangeStatus_ShouldUpdateAndTouchUpdatedAt()
    {
        var r = WorkoutRoutine.Create(1, "Treino", null, DateOnly.FromDateTime(DateTime.UtcNow), 30);
        var before = r.UpdatedAt;

        Thread.Sleep(10);
        r.ChangeStatus(WorkoutStatus.Completed);

        r.Status.Should().Be(WorkoutStatus.Completed);
        r.UpdatedAt.Should().BeAfter(before);
    }
}
