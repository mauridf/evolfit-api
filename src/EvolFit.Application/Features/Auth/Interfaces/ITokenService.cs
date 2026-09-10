namespace EvolFit.Application.Features.Auth.Interfaces;

public interface ITokenService
{
    /// <summary>Gera o access token JWT (2h).</summary>
    string GenerateAccessToken(int userId, string username, string email);

    /// <summary>Gera um refresh token opaco (string aleatória, 7 dias).</summary>
    string GenerateRefreshToken();

    /// <summary>Gera o hash SHA-256 do refresh token para persistência.</summary>
    string HashRefreshToken(string refreshToken);
}
