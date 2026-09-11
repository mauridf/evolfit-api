using EvolFit.Application.Features.Wger.DTOs;
using EvolFit.Application.Features.Wger.Interfaces;
using EvolFit.Application.Features.Workouts;
using EvolFit.Application.Features.Workouts.DTOs;
using EvolFit.Core.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace EvolFit.UnitTests.Services;

public class WorkoutGeneratorServiceTests
{
    private readonly IWgerExerciseClient _wger = Substitute.For<IWgerExerciseClient>();

    [Fact]
    public async Task Generate_ShouldDistributeRoundRobin()
    {
        // 3 exercícios disponíveis por músculo
        var list = new List<ExerciseListItemDto>
        {
            new(1, "Bench Press", "desc", "Chest", new(), new()),
            new(2, "Incline Press", "desc", "Chest", new(), new()),
            new(3, "Cable Fly", "desc", "Chest", new(), new()),
        };

        _wger.GetExercisesByMuscleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns(list);

        var sut = new WorkoutGeneratorService(_wger, NullLogger<WorkoutGeneratorService>.Instance);

        var req = new GenerateWorkoutRequest(
            "Treino", "hypertrophy", 7,
            new List<string> { "chest" }, "intermediate");

        var (routine, exercises) = await sut.GenerateAsync(1, req);

        exercises.Should().HaveCount(21); // 7 dias × 3 exercícios
        routine.TotalDays.Should().Be(7);

        // Dia 1 tem 3 exercícios com order 1,2,3
        exercises.Where(e => e.DayNumber == 1)
            .Select(e => e.OrderInDay)
            .Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Generate_WithNoExercisesFromWger_ShouldThrow()
    {
        _wger.GetExercisesByMuscleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns(new List<ExerciseListItemDto>());

        var sut = new WorkoutGeneratorService(_wger, NullLogger<WorkoutGeneratorService>.Instance);

        var act = () => sut.GenerateAsync(1, new GenerateWorkoutRequest(
            "Treino", "hypertrophy", 7, new List<string> { "chest" }, "intermediate"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
