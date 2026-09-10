namespace EvolFit.Application.Features.Auth.DTOs;

// ---------- Requests ----------
public sealed record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string DisplayName,
    DateOnly? BirthDate);

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record UpdateProfileRequest(string DisplayName, DateOnly? BirthDate);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

// ---------- Responses ----------
public sealed record RegisterResponse(
    int Id,
    string Username,
    string Email,
    string AccessToken,
    string RefreshToken);

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserSummary User);

public sealed record UserSummary(
    int Id,
    string Username,
    string Email,
    string DisplayName);

public sealed record RefreshResponse(string AccessToken, string RefreshToken);

public sealed record ProfileResponse(
    int Id,
    string Username,
    string Email,
    string DisplayName,
    DateOnly? BirthDate,
    DateTime CreatedAt);
