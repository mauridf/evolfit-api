using EvolFit.Application.Features.Workouts.Mappers;
using EvolFit.Core.Enums;
using FluentAssertions;

namespace EvolFit.UnitTests.Core;

public class BodyPartMuscleMapperTests
{
    [Theory]
    [InlineData(BodyPart.Chest, 4)]       // Pectoralis Major
    [InlineData(BodyPart.Back, 12)]       // Latissimus Dorsi
    [InlineData(BodyPart.Legs, 10)]       // Quadriceps Femoris
    [InlineData(BodyPart.Glutes, 8)]      // Gluteus Maximus
    [InlineData(BodyPart.Shoulders, 2)]   // Anterior Deltoid
    [InlineData(BodyPart.Arms, 1)]        // Biceps Brachii
    [InlineData(BodyPart.Abs, 6)]         // Rectus Abdominis
    public void ToMuscleId_ShouldMatchWgerApi(BodyPart part, int expected) =>
        BodyPartMuscleMapper.ToMuscleId(part).Should().Be(expected);

    [Fact]
    public void ToMuscleIds_ShouldExcludeCardio()
    {
        var ids = BodyPartMuscleMapper.ToMuscleIds(new[]
        {
            BodyPart.Chest, BodyPart.Cardio, BodyPart.Shoulders
        });

        ids.Should().BeEquivalentTo(new[] { 4, 2 });
    }

    [Fact]
    public void ToMuscleId_ForCardio_ShouldThrow()
    {
        var act = () => BodyPartMuscleMapper.ToMuscleId(BodyPart.Cardio);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CardioCategoryId_ShouldBe15() =>
        BodyPartMuscleMapper.CardioCategoryId.Should().Be(15); // wger exercisecategory Cardio
}