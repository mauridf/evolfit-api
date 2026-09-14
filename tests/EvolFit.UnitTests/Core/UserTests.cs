using EvolFit.Core.Entities;
using FluentAssertions;

namespace EvolFit.UnitTests.Core;

public class UserTests
{
    [Fact]
    public void Create_ShouldNormalizeEmailAndTrimFields()
    {
        var user = User.Create(
            "  carlos  ",
            "  Carlos@Email.COM  ",
            "hash",
            "  Carlos Silva  ",
            new DateOnly(1998, 5, 15));

        user.Username.Should().Be("carlos");
        user.Email.Should().Be("carlos@email.com");
        user.DisplayName.Should().Be("Carlos Silva");
    }

    [Fact]
    public void UpdateProfile_ShouldChangeFieldsAndTouchUpdatedAt()
    {
        var user = User.Create("c", "c@e.com", "h", "Carlos", null);
        var before = user.UpdatedAt;

        Thread.Sleep(10);
        user.UpdateProfile("Carlos Silva Santos", new DateOnly(1998, 5, 15));

        user.DisplayName.Should().Be("Carlos Silva Santos");
        user.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void ChangePassword_ShouldReplaceHash()
    {
        var user = User.Create("c", "c@e.com", "old-hash", "Carlos", null);
        user.ChangePassword("new-hash");
        user.PasswordHash.Should().Be("new-hash");
    }
}
