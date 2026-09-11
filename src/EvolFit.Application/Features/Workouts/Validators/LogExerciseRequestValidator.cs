using EvolFit.Application.Features.Workouts.DTOs;
using FluentValidation;

namespace EvolFit.Application.Features.Workouts.Validators;

public class LogExerciseRequestValidator : AbstractValidator<LogExerciseRequest>
{
    public LogExerciseRequestValidator()
    {
        RuleFor(x => x.WorkoutExerciseId)
            .GreaterThan(0);

        RuleFor(x => x.WeightUsed)
            .GreaterThanOrEqualTo(0)
            .When(x => x.WeightUsed.HasValue);
    }
}
