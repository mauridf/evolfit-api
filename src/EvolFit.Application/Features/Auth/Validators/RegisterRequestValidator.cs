using EvolFit.Application.Features.Auth.DTOs;
using FluentValidation;

namespace EvolFit.Application.Features.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username é obrigatório.")
            .Length(3, 100).WithMessage("Username deve ter entre 3 e 100 caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido (VALID-004).")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres (VALID-003).")
            .Matches("[A-Z]").WithMessage("Senha deve ter pelo menos 1 letra maiúscula (VALID-003).")
            .Matches("[a-z]").WithMessage("Senha deve ter pelo menos 1 letra minúscula (VALID-003).")
            .Matches("[0-9]").WithMessage("Senha deve ter pelo menos 1 número (VALID-003).");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Nome de exibição é obrigatório.")
            .Length(3, 255);

        RuleFor(x => x.BirthDate)
            .Must(BeValidAge).WithMessage("Idade deve estar entre 10 e 120 anos.");
    }

    private static bool BeValidAge(DateOnly? birthDate)
    {
        if (birthDate is null) return true;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value > today.AddYears(-age)) age--;

        return age is >= 10 and <= 120;
    }
}
