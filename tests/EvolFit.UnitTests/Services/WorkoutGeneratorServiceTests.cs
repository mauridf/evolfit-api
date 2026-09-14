using EvolFit.Application.Features.Wger.DTOs;
using EvolFit.Application.Features.Wger.Interfaces;
using EvolFit.Application.Features.Workouts;
using EvolFit.Application.Features.Workouts.DTOs;
using EvolFit.Core.Entities;
using EvolFit.Core.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace EvolFit.UnitTests.Services;

public class WorkoutGeneratorServiceTests
{
    private readonly IWgerExerciseClient _wger = Substitute.For<IWgerExerciseClient>();
    private readonly IWgerExerciseCacheRepository _cache = Substitute.For<IWgerExerciseCacheRepository>();

    private WorkoutGeneratorService CreateSut() =>
        new(_wger, _cache, NullLogger<WorkoutGeneratorService>.Instance);

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

        var sut = CreateSut();

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

        var sut = CreateSut();

        var act = () => sut.GenerateAsync(1, new GenerateWorkoutRequest(
            "Treino", "hypertrophy", 7, new List<string> { "chest" }, "intermediate"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData("beginner", 3, 10)]
    [InlineData("intermediate", 3, 10)]
    [InlineData("advanced", 4, 12)]
    public async Task Generate_ShouldApplyDifficultyPrescription(
        string difficulty, int expectedSets, int expectedReps)
    {
        var list = new List<ExerciseListItemDto>
        {
            new(5, "Squat", "desc", "Legs", new(), new()),
        };

        _wger.GetExercisesByMuscleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns(list);

        var sut = CreateSut();

        var (_, exercises) = await sut.GenerateAsync(1, new GenerateWorkoutRequest(
            "Treino", "hypertrophy", 7, new List<string> { "legs" }, difficulty));

        exercises.Should().OnlyContain(e => e.Sets == expectedSets && e.Reps == expectedReps);
    }

    [Fact]
    public async Task Generate_ShouldUpsertFetchedExercisesInCache()
    {
        var list = new List<ExerciseListItemDto>
        {
            new(10, "Deadlift", "desc", "Back", new(), new()),
        };

        _wger.GetExercisesByMuscleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns(list);

        _cache.GetByWgerIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
              .Returns((WgerExerciseCache?)null);

        var sut = CreateSut();

        await sut.GenerateAsync(1, new GenerateWorkoutRequest(
            "Treino", "strength", 7, new List<string> { "back" }, "intermediate"));

        await _cache.Received(1).AddAsync(
            Arg.Is<WgerExerciseCache>(e =>
                e.WgerExerciseId == 10 && e.MuscleId == 12), // back → Latissimus Dorsi
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_WhenWgerFails_ShouldFallbackToCachedExercises()
    {
        var cached = WgerExerciseCache.Create(
            15, "Squat", "desc", "Legs", "[\"Quadriceps\"]", "[]", null,
            TimeSpan.FromDays(7), muscleId: 10); // legs

        _wger.GetExercisesByMuscleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns<IReadOnlyList<ExerciseListItemDto>>(_ => throw new HttpRequestException("wger indisponível"));
        _cache.GetByMuscleIdAsync(10, Arg.Any<CancellationToken>())
              .Returns(new List<WgerExerciseCache> { cached });

        var sut = CreateSut();

        var (_, exercises) = await sut.GenerateAsync(1, new GenerateWorkoutRequest(
            "Treino", "strength", 7, new List<string> { "legs" }, "intermediate"));

        exercises.Should().HaveCount(21); // 7 dias × 3 exercícios (round-robin do cache)
        exercises.Should().OnlyContain(e => e.ExerciseName == "Squat");
        await _cache.Received(1).GetByMuscleIdAsync(10, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_WhenCardioFetchFails_ShouldFallbackByCategory()
    {
        var cached = WgerExerciseCache.Create(
            177, "Cycling", "desc", "Cardio", "[]", "[]", null,
            TimeSpan.FromDays(7), categoryId: 15);

        _wger.GetExercisesByCategoryAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns<IReadOnlyList<ExerciseListItemDto>>(_ => throw new HttpRequestException("wger indisponível"));
        _cache.GetByCategoryIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
              .Returns(new List<WgerExerciseCache> { cached });

        var sut = CreateSut();

        var (_, exercises) = await sut.GenerateAsync(1, new GenerateWorkoutRequest(
            "Cardio", "cardio", 7, new List<string> { "cardio" }, "beginner"));

        exercises.Should().HaveCount(21);
        await _cache.Received(1).GetByCategoryIdAsync(
            EvolFit.Application.Features.Workouts.Mappers.BodyPartMuscleMapper.CardioCategoryId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generate_WithCardio_ShouldQueryByCategory()
    {
        var list = new List<ExerciseListItemDto>
        {
            new(177, "Cycling", "desc", "Cardio", new(), new()),
        };

        _wger.GetExercisesByCategoryAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
             .Returns(list);

        var sut = CreateSut();

        var (_, exercises) = await sut.GenerateAsync(1, new GenerateWorkoutRequest(
            "Treino Cardio", "cardio", 7, new List<string> { "cardio" }, "beginner"));

        await _wger.Received(1).GetExercisesByCategoryAsync(
            EvolFit.Application.Features.Workouts.Mappers.BodyPartMuscleMapper.CardioCategoryId,
            Arg.Any<CancellationToken>());

        exercises.Should().HaveCount(21); // 7 dias × 3 exercícios
        exercises.Should().OnlyContain(e => e.ExerciseName == "Cycling");
    }
}