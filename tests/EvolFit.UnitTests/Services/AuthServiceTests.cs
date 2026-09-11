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
