using EvolFit.Application.Common;
using EvolFit.Application.Common.Exceptions;
using EvolFit.Application.Features.Auth.DTOs;
using EvolFit.Application.Features.Auth.Interfaces;
using EvolFit.Core.Entities;

namespace EvolFit.Application.Features.Auth;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<RefreshResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);
    Task<ProfileResponse> GetProfileAsync(CancellationToken ct = default);
    Task<ProfileResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _tokens;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;

    // Injetado para configuração de expiração dos tokens
    private readonly int _refreshTokenExpireDays;
    private readonly int _accessTokenExpireSeconds;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository tokens,
        IPasswordHasher hasher,
        ITokenService tokenService,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        Microsoft.Extensions.Options.IOptions<EvolFit.Application.Features.Auth.AuthOptions> options)
    {
        _users = users;
        _tokens = tokens;
        _hasher = hasher;
        _tokenService = tokenService;
        _uow = uow;
        _currentUser = currentUser;
        _refreshTokenExpireDays = options.Value.RefreshTokenExpireDays;
        _accessTokenExpireSeconds = options.Value.ExpireMinutes * 60;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _users.EmailExistsAsync(request.Email, ct))
            throw new ConflictException("E-mail já cadastrado.");

        if (await _users.UsernameExistsAsync(request.Username, ct))
            throw new ConflictException("Username já está em uso.");

        var passwordHash = _hasher.Hash(request.Password);

        var user = User.Create(
            request.Username,
            request.Email,
            passwordHash,
            request.DisplayName,
            request.BirthDate);

        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Email);
        var refreshToken = await IssueRefreshTokenAsync(user.Id, ct);

        return new RegisterResponse(user.Id, user.Username, user.Email, accessToken, refreshToken);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct)
            ?? throw new UnauthorizedException("Credenciais inválidas.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Credenciais inválidas.");

        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Email);
        var refreshToken = await IssueRefreshTokenAsync(user.Id, ct);

        return new LoginResponse(
            accessToken,
            refreshToken,
            ExpiresIn: _accessTokenExpireSeconds,
            new UserSummary(user.Id, user.Username, user.Email, user.DisplayName));
    }

    public async Task<RefreshResponse> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var hash = _tokenService.HashRefreshToken(request.RefreshToken);
        var stored = await _tokens.GetByHashAsync(hash, ct)
            ?? throw new UnauthorizedException("Refresh token inválido.");

        // Detecção de replay: token já revogado → revoga TODA a família do usuário
        if (stored.RevokedAt is not null)
        {
            await _tokens.RevokeAllForUserAsync(stored.UserId, ct);
            await _uow.SaveChangesAsync(ct);
            throw new ConflictException("Refresh token já utilizado. Sessões revogadas por segurança.");
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token expirado.");

        var user = await _users.GetByIdAsync(stored.UserId, ct)
            ?? throw new UnauthorizedException("Usuário não encontrado.");

        // Cria o novo par (rotação)
        var newAccess = _tokenService.GenerateAccessToken(user.Id, user.Username, user.Email);
        var newRefreshPlain = _tokenService.GenerateRefreshToken();
        var newRefreshHash = _tokenService.HashRefreshToken(newRefreshPlain);

        var newToken = RefreshToken.Create(
            user.Id,
            newRefreshHash,
            DateTime.UtcNow.AddDays(_refreshTokenExpireDays));

        await _tokens.AddAsync(newToken, ct);
        await _uow.SaveChangesAsync(ct);       // gera o Id do novo token

        stored.Revoke(newToken.Id);
        _tokens.Update(stored);
        await _uow.SaveChangesAsync(ct);

        return new RefreshResponse(newAccess, newRefreshPlain);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var hash = _tokenService.HashRefreshToken(request.RefreshToken);
        var stored = await _tokens.GetByHashAsync(hash, ct);

        if (stored is null || stored.RevokedAt is not null)
            return; // idempotente: já revogado ou inexistente

        stored.Revoke();
        _tokens.Update(stored);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<ProfileResponse> GetProfileAsync(CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
            ?? throw new NotFoundException("Usuário não encontrado.");

        return MapProfile(user);
    }

    public async Task<ProfileResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
            ?? throw new NotFoundException("Usuário não encontrado.");

        user.UpdateProfile(request.DisplayName, request.BirthDate);
        _users.Update(user);
        await _uow.SaveChangesAsync(ct);

        return MapProfile(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId, ct)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (!_hasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Senha atual incorreta.");

        user.ChangePassword(_hasher.Hash(request.NewPassword));
        _users.Update(user);

        // Revoga todas as sessões ativas por segurança
        await _tokens.RevokeAllForUserAsync(user.Id, ct);

        await _uow.SaveChangesAsync(ct);
    }

    // ---------- Helpers ----------
    private async Task<string> IssueRefreshTokenAsync(int userId, CancellationToken ct)
    {
        var plain = _tokenService.GenerateRefreshToken();
        var hash = _tokenService.HashRefreshToken(plain);

        var token = RefreshToken.Create(
            userId,
            hash,
            DateTime.UtcNow.AddDays(_refreshTokenExpireDays));

        await _tokens.AddAsync(token, ct);
        await _uow.SaveChangesAsync(ct);

        return plain;
    }

    private static ProfileResponse MapProfile(User user) =>
        new(user.Id, user.Username, user.Email, user.DisplayName, user.BirthDate, user.CreatedAt);
}

public class AuthOptions
{
    public int RefreshTokenExpireDays { get; set; } = 7;
    public int ExpireMinutes { get; set; } = 120;
}
