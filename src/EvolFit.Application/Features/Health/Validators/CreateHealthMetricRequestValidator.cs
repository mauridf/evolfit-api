using EvolFit.Application.Features.Health.DTOs;
using FluentValidation;

namespace EvolFit.Application.Features.Health.Validators;

public class CreateHealthMetricRequestValidator : AbstractValidator<CreateHealthMetricRequest>
{
    private static readonly string[] ValidActivityLevels =
        ["sedentary", "light", "moderate", "active", "extreme"];

    private static readonly string[] ValidGenders = ["male", "female"];

    public CreateHealthMetricRequestValidator()
    {
        RuleFor(x => x.WeightKg)
            .InclusiveBetween(40, 300)
            .WithMessage("Peso deve estar entre 40 e 300 kg (VALID-001).");

        RuleFor(x => x.HeightCm)
            .InclusiveBetween(100, 250)
            .WithMessage("Altura deve estar entre 100 e 250 cm (VALID-001).");

        RuleFor(x => x.Age)
            .InclusiveBetween(10, 120)
            .WithMessage("Idade deve estar entre 10 e 120 anos (VALID-001).");

        RuleFor(x => x.Gender)
            .Must(g => ValidGenders.Contains(g.ToLowerInvariant()))
            .WithMessage("Gênero deve ser 'male' ou 'female'.");

        RuleFor(x => x.ActivityLevel)
            .Must(a => ValidActivityLevels.Contains(a.ToLowerInvariant()))
            .WithMessage("Nível de atividade inválido.");
    }
}
