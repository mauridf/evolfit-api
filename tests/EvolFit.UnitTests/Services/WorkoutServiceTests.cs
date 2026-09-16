using EvolFit.Application.Common;
using EvolFit.Application.Common.Exceptions;
using EvolFit.Application.Features.Workouts;
using EvolFit.Application.Features.Workouts.DTOs;
using EvolFit.Application.Features.Workouts.Interfaces;
using EvolFit.Core.Entities;
using EvolFit.Core.Enums;
using FluentAssertions;
using NSubstitute;

namespace EvolFit.UnitTests.Services;

public class WorkoutServiceTests
{
    private readonly IWorkoutRoutineRepository _routines = Substitute.For<IWorkoutRoutineRepository>();
    private readonly IWorkoutExerciseRepository _exercises = Substitute.For<IWorkoutExerciseRepository>();
    private readonly IExerciseLogRepository _logs = Substitute.For<IExerciseLogRepository>();
    private readonly IWorkoutGeneratorService _generator = Substitute.For<IWorkoutGeneratorService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private WorkoutService BuildSut()
    {
        _currentUser.UserId.Returns(1);
        return new WorkoutService(_routines, _exercises, _logs, _generator, _uow, _currentUser);
    }

    [Fact]
    public async Task UpdateStatus_ActivateWhileAnotherActive_ShouldThrowConflict()
    {
        var routine = WorkoutRoutine.Create(
            1, "Treino", Goal.Hypertrophy, DateOnly.FromDateTime(DateTime.UtcNow), 30);
        routine.ChangeStatus(WorkoutStatus.Paused);

        var otherActive = WorkoutRoutine.Create(
            1, "Outra", Goal.Strength, DateOnly.FromDateTime(DateTime.UtcNow), 30);

        _routines.GetWithExercisesForUserAsync(10, 1, Arg.Any<CancellationToken>()).Returns(routine);
        _routines.GetActiveForUserAsync(1, Arg.Any<CancellationToken>()).Returns(otherActive);

        var sut = BuildSut();
        var act = () => sut.UpdateStatusAsync(10, new UpdateWorkoutStatusRequest((int)WorkoutStatus.Active));

        var ex = await act.Should().ThrowAsync<ConflictException>();
        ex.Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task UpdateStatus_ActivateAlreadyActiveRoutine_ShouldSucceed()
    {
        var routine = WorkoutRoutine.Create(
            1, "Treino", Goal.Hypertrophy, DateOnly.FromDateTime(DateTime.UtcNow), 30);

        _routines.GetWithExercisesForUserAsync(10, 1, Arg.Any<CancellationToken>()).Returns(routine);
        _routines.GetActiveForUserAsync(1, Arg.Any<CancellationToken>()).Returns(routine);

        var sut = BuildSut();

        var result = await sut.UpdateStatusAsync(10, new UpdateWorkoutStatusRequest((int)WorkoutStatus.Active));

        result.Status.Should().Be((int)WorkoutStatus.Active);
    }

    [Theory]
    [InlineData("active", WorkoutStatus.Active)]
    [InlineData("PAUSED", WorkoutStatus.Paused)]
    [InlineData("completed", WorkoutStatus.Completed)]
    [InlineData("1", WorkoutStatus.Active)]
    [InlineData("", null)]
    public async Task List_WithStatusFilter_ShouldForwardParsedStatus(
        string? filter, WorkoutStatus? expected)
    {
        _routines.GetPagedForUserAsync(1, 1, 20, Arg.Any<WorkoutStatus?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(((IReadOnlyList<WorkoutRoutine>)Array.Empty<WorkoutRoutine>(), 0)));

        var sut = BuildSut();

        await sut.ListAsync(1, 20, filter);

        await _routines.Received(1).GetPagedForUserAsync(1, 1, 20, expected, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_WithInvalidStatus_ShouldThrowValidation422()
    {
        var sut = BuildSut();
        var act = () => sut.ListAsync(1, 20, "bogus");

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.StatusCode.Should().Be(422);
    }
}