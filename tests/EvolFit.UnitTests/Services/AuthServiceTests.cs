using EvolFit.Application.Common;
using EvolFit.Application.Common.Exceptions;
using EvolFit.Application.Features.Auth;
using EvolFit.Application.Features.Auth.DTOs;
using EvolFit.Application.Features.Auth.Interfaces;
using EvolFit.Core.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace EvolFit.UnitTests.Services;

public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _tokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokens2 = Substitute.For<ITokenService>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private AuthService BuildSut() => new(
        _users, _tokens, _hasher, _tokens2, _uow, _currentUser,
        Options.Create(new AuthOptions { RefreshTokenExpireDays = 7 }));

    [Fact]
    public async Task Register_WhenEmailExists_ThrowsConflict()
    {
        _users.EmailExistsAsync("c@e.com", Arg.Any<CancellationToken>()).Returns(true);

        var sut = BuildSut();
        var act = () => sut.RegisterAsync(
            new RegisterRequest("c", "c@e.com", "S3nh@F0rte!", "Carlos", null));

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Register_WhenValid_ReturnsTokensAndPersists()
    {
        _users.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.UsernameExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        _hasher.Hash("S3nh@F0rte!").Returns("hashed");
        _tokens2.GenerateAccessToken(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>()).Returns("access");
        _tokens2.GenerateRefreshToken().Returns("refresh-plain");
        _tokens2.HashRefreshToken("refresh-plain").Returns("refresh-hash");

        var sut = BuildSut();
        var result = await sut.RegisterAsync(
            new RegisterRequest("carlos", "c@e.com", "S3nh@F0rte!", "Carlos", null));

        result.AccessToken.Should().Be("access");
        result.RefreshToken.Should().Be("refresh-plain");

        await _users.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await _tokens.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
        await _uow.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorized()
    {
        var user = User.Create("c", "c@e.com", "hash", "Carlos", null);
        _users.GetByEmailAsync("c@e.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("errado", "hash").Returns(false);

        var sut = BuildSut();
        var act = () => sut.LoginAsync(new LoginRequest("c@e.com", "errado"));

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_WithWrongPassword_IncrementsFailureCounter()
    {
        var user = CreateUserWithFailedLogins(2);
        _users.GetByEmailAsync("c@e.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), "hash").Returns(false);

        var sut = BuildSut();
        var act = () => sut.LoginAsync(new LoginRequest("c@e.com", "errado"));

        await act.Should().ThrowAsync<UnauthorizedException>();
        user.FailedLoginCount.Should().Be(3);
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_FifthFailedAttempt_LocksOutAccount()
    {
        var user = CreateUserWithFailedLogins(4);
        _users.GetByEmailAsync("c@e.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify(Arg.Any<string>(), "hash").Returns(false);

        var sut = BuildSut();
        var act = () => sut.LoginAsync(new LoginRequest("c@e.com", "errado"));

        await act.Should().ThrowAsync<UnauthorizedException>();
        user.IsLockedOut(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public async Task Login_WhenAccountLockedOut_ThrowsUnauthorized_WithoutCheckingPassword()
    {
        var user = CreateUserWithFailedLogins(5); // 5ª falha bloqueia a conta
        _users.GetByEmailAsync("c@e.com", Arg.Any<CancellationToken>()).Returns(user);

        var sut = BuildSut();
        var act = () => sut.LoginAsync(
            new LoginRequest("c@e.com", "senha-correta-mas-bloqueada"));

        await act.Should().ThrowAsync<UnauthorizedException>();
        _hasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ResetsFailuresAndLockout()
    {
        var user = CreateUserWithFailedLogins(2);
        _users.GetByEmailAsync("c@e.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("S3nh@F0rte!", "hash").Returns(true);
        _tokens2.GenerateAccessToken(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>()).Returns("access");
        _tokens2.GenerateRefreshToken().Returns("refresh-plain");
        _tokens2.HashRefreshToken("refresh-plain").Returns("refresh-hash");

        var sut = BuildSut();
        var result = await sut.LoginAsync(new LoginRequest("c@e.com", "S3nh@F0rte!"));

        result.AccessToken.Should().Be("access");
        user.FailedLoginCount.Should().Be(0);
        user.LockoutUntil.Should().BeNull();
    }

    private static User CreateUserWithFailedLogins(int failures)
    {
        var user = User.Create("c", "c@e.com", "hash", "Carlos", null);
        for (var i = 0; i < failures; i++)
            user.RegisterFailedLogin(5, TimeSpan.FromMinutes(15));
        return user;
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_RevokesAllAndThrowsConflict()
    {
        var stored = RefreshToken.Create(1, "hash", DateTime.UtcNow.AddDays(1));
        stored.Revoke(); // simula replay

        _tokens2.HashRefreshToken("plain").Returns("hash");
        _tokens.GetByHashAsync("hash", Arg.Any<CancellationToken>()).Returns(stored);

        var sut = BuildSut();
        var act = () => sut.RefreshAsync(new RefreshRequest("plain"));

        await act.Should().ThrowAsync<ConflictException>();
        await _tokens.Received(1).RevokeAllForUserAsync(1, Arg.Any<CancellationToken>());
    }
}
