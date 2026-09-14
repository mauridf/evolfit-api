using EvolFit.Core.ValueObjects;
using FluentAssertions;

namespace EvolFit.UnitTests.Core;

public class EmailTests
{
    [Theory]
    [InlineData("carlos@email.com")]
    [InlineData("CARLOS@EMAIL.COM")]
    public void Create_WithValidEmail_ShouldNormalize(string raw)
    {
        var email = Email.Create(raw);
        email.Value.Should().Be("carlos@email.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("sem-arroba")]
    [InlineData("@semlocal.com")]
    [InlineData("a@b")]
    public void Create_WithInvalidEmail_ShouldThrow(string raw)
    {
        var act = () => Email.Create(raw);
        act.Should().Throw<ArgumentException>();
    }
}
