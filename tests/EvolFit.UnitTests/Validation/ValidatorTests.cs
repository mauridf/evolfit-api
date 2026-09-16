using EvolFit.Application.Features.Auth.DTOs;
using EvolFit.Application.Features.Auth.Validators;
using EvolFit.Application.Features.Health.DTOs;
using EvolFit.Application.Features.Health.Validators;
using EvolFit.Application.Features.Workouts.DTOs;
using EvolFit.Application.Features.Workouts.Validators;
using FluentValidation.TestHelper;

namespace EvolFit.UnitTests.Validation;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest Valid() =>
        new("joao", "joao@example.com", "SenhaForte123", "João", new DateOnly(1995, 5, 10));

    [Fact]
    public void Validate_WhenPasswordHasNoUppercase_ShouldReportValidacionError()
    {
        var request = Valid() with { Password = "senhaforte123" };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WhenPasswordHasNoLowercase_ShouldReportValidationError()
    {
        var request = Valid() with { Password = "SENHAFORTE123" };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WhenPasswordHasNoDigit_ShouldReportValidationError()
    {
        var request = Valid() with { Password = "SenhaForte" };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WhenPasswordTooShort_ShouldReportValidationError()
    {
        var request = Valid() with { Password = "Senha12" };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WhenEmailInvalid_ShouldReportValidationError()
    {
        var request = Valid() with { Email = "joao@" };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WhenUsernameTooShort_ShouldReportValidationError()
    {
        var request = Valid() with { Username = "jo" };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void Validate_WhenBirthDateUnderAgeTen_ShouldReportValidationError()
    {
        var request = Valid() with { BirthDate = new DateOnly(2025, 1, 1) };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.BirthDate);
    }

    [Fact]
    public void Validate_WhenRequestValid_ShouldNotHaveValidationErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
}

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenEmailEmpty_ShouldReportValidationError() =>
        _validator.TestValidate(new LoginRequest(string.Empty, "SenhaForte123"))
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Validate_WhenEmailInvalid_ShouldReportValidationError() =>
        _validator.TestValidate(new LoginRequest("joao@", "SenhaForte123"))
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Validate_WhenPasswordEmpty_ShouldReportValidationError() =>
        _validator.TestValidate(new LoginRequest("joao@example.com", string.Empty))
            .ShouldHaveValidationErrorFor(x => x.Password);

    [Fact]
    public void Validate_WhenRequestValid_ShouldNotHaveValidationErrors() =>
        _validator.TestValidate(new LoginRequest("joao@example.com", "SenhaForte123"))
            .ShouldNotHaveAnyValidationErrors();
}

public class ChangePasswordRequestValidatorTests
{
    private readonly ChangePasswordRequestValidator _validator = new();

    [Fact]
    public void Validate_WhenCurrentPasswordEmpty_ShouldReportValidationError() =>
        _validator.TestValidate(new ChangePasswordRequest(string.Empty, "SenhaForte123"))
            .ShouldHaveValidationErrorFor(x => x.CurrentPassword);

    [Fact]
    public void Validate_WhenNewPasswordHasNoUppercase_ShouldReportValidationError() =>
        _validator.TestValidate(new ChangePasswordRequest("SenhaAtual123", "senhaforte123"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);

    [Fact]
    public void Validate_WhenNewPasswordTooShort_ShouldReportValidationError() =>
        _validator.TestValidate(new ChangePasswordRequest("SenhaAtual123", "Senha1"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);

    [Fact]
    public void Validate_WhenRequestValid_ShouldNotHaveValidationErrors() =>
        _validator.TestValidate(new ChangePasswordRequest("SenhaAtual123", "SenhaForte123"))
            .ShouldNotHaveAnyValidationErrors();
}

public class GenerateWorkoutRequestValidatorTests
{
    private readonly GenerateWorkoutRequestValidator _validator = new();

    private static GenerateWorkoutRequest Valid() =>
        new("Rotina A", "strength", 30, ["chest", "back"], "beginner");

    [Fact]
    public void Validate_WhenGoalInvalid_ShouldReportValidationError()
    {
        var request = Valid() with { Goal = "powerlifting" };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Goal);
    }

    [Fact]
    public void Validate_WhenBodyPartInvalid_ShouldReportValidationError()
    {
        var request = Valid() with { BodyParts = ["chest", "biceps"] };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.BodyParts);
    }

    [Fact]
    public void Validate_WhenPeriodDaysBelowMinimum_ShouldReportValidationError()
    {
        var request = Valid() with { PeriodDays = 5 };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.PeriodDays);
    }

    [Fact]
    public void Validate_WhenPeriodDaysAboveMaximum_ShouldReportValidationError()
    {
        var request = Valid() with { PeriodDays = 200 };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.PeriodDays);
    }

    [Fact]
    public void Validate_WhenDifficultyInvalid_ShouldReportValidationError()
    {
        var request = Valid() with { Difficulty = "expert" };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Difficulty);
    }

    [Fact]
    public void Validate_WhenRequestValid_ShouldNotHaveValidationErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
}

public class CreateHealthMetricRequestValidatorTests
{
    private readonly CreateHealthMetricRequestValidator _validator = new();

    private static CreateHealthMetricRequest Valid() =>
        new(80m, 180m, "male", 30, "moderate");

    [Fact]
    public void Validate_WhenWeightBelowMinimum_ShouldReportValidationError()
    {
        var request = Valid() with { WeightKg = 39m };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void Validate_WhenWeightAboveMaximum_ShouldReportValidationError()
    {
        var request = Valid() with { WeightKg = 301m };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.WeightKg);
    }

    [Fact]
    public void Validate_WhenHeightBelowMinimum_ShouldReportValidationError()
    {
        var request = Valid() with { HeightCm = 99m };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.HeightCm);
    }

    [Fact]
    public void Validate_WhenHeightAboveMaximum_ShouldReportValidationError()
    {
        var request = Valid() with { HeightCm = 251m };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.HeightCm);
    }

    [Fact]
    public void Validate_WhenAgeBelowMinimum_ShouldReportValidationError()
    {
        var request = Valid() with { Age = 9 };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Age);
    }

    [Fact]
    public void Validate_WhenAgeAboveMaximum_ShouldReportValidationError()
    {
        var request = Valid() with { Age = 121 };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Age);
    }

    [Fact]
    public void Validate_WhenGenderInvalid_ShouldReportValidationError()
    {
        var request = Valid() with { Gender = "other" };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public void Validate_WhenActivityLevelInvalid_ShouldReportValidationError()
    {
        var request = Valid() with { ActivityLevel = "lazy" };

        _validator.TestValidate(request).ShouldHaveValidationErrorFor(x => x.ActivityLevel);
    }

    [Fact]
    public void Validate_WhenRequestValid_ShouldNotHaveValidationErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
}