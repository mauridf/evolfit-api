using EvolFit.Application.Features.Workouts.DTOs;
using FluentValidation;

namespace EvolFit.Application.Features.Workouts.Validators;

public class GenerateWorkoutRequestValidator : AbstractValidator<GenerateWorkoutRequest>
{
    private static readonly string[] ValidGoals =
        ["strength", "hypertrophy", "endurance", "flexibility", "cardio"];

    private static readonly string[] ValidDifficulties =
        ["beginner", "intermediate", "advanced"];

    private static readonly string[] ValidBodyParts =
        ["chest", "back", "legs", "glutes", "shoulders", "arms", "abs", "cardio"];

    public GenerateWorkoutRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .Length(3, 255);

        RuleFor(x => x.Goal)
            .Must(g => ValidGoals.Contains(g.ToLowerInvariant()))
            .WithMessage("Objetivo inválido.");

        RuleFor(x => x.PeriodDays)
            .InclusiveBetween(7, 180)
            .WithMessage("Período deve estar entre 7 e 180 dias (VALID-002).");

        RuleFor(x => x.BodyParts)
            .NotEmpty().WithMessage("Selecione ao menos uma parte do corpo.")
            .Must(bp => bp.All(p => ValidBodyParts.Contains(p.ToLowerInvariant())))
            .WithMessage("Parte do corpo inválida.");

        RuleFor(x => x.Difficulty)
            .Must(d => ValidDifficulties.Contains(d.ToLowerInvariant()))
            .WithMessage("Dificuldade inválida.");
    }
}
