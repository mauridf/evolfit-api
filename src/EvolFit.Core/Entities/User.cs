namespace EvolFit.Core.Entities;

/// <summary>
/// Usuário do sistema — agregado raiz do bounded context Auth.
/// </summary>
public class User
{
    public int Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public DateOnly? BirthDate { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTime? LockoutUntil { get; private set; }

    // Navegação
    public ICollection<HealthMetric> HealthMetrics { get; private set; } = new List<HealthMetric>();
    public ICollection<WorkoutRoutine> WorkoutRoutines { get; private set; } = new List<WorkoutRoutine>();
    public ICollection<ExerciseLog> ExerciseLogs { get; private set; } = new List<ExerciseLog>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    // Construtor privado para EF Core
    private User() { }

    /// <summary>
    /// Cria um novo usuário (usado no cadastro).
    /// </summary>
    public static User Create(
        string username,
        string email,
        string passwordHash,
        string displayName,
        DateOnly? birthDate)
    {
        return new User
        {
            Username = username.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            DisplayName = displayName.Trim(),
            BirthDate = birthDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Atualiza dados de perfil (PUT /auth/profile).
    /// </summary>
    public void UpdateProfile(string displayName, DateOnly? birthDate)
    {
        DisplayName = displayName.Trim();
        BirthDate = birthDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Substitui o hash de senha (change-password).
    /// </summary>
    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Conta bloqueada após 5 falhas consecutivas (SECURITY §7 — janela 15 min).
    /// </summary>
    public bool IsLockedOut(DateTime utcNow)
    {
        return LockoutUntil is not null && LockoutUntil.Value > utcNow;
    }

    /// <summary>
    /// Registra uma falha de login; ao atingir o limite, bloqueia a conta.
    /// </summary>
    public void RegisterFailedLogin(int maxAttempts, TimeSpan lockoutDuration)
    {
        var now = DateTime.UtcNow;
        FailedLoginCount++;

        if (FailedLoginCount >= maxAttempts)
        {
            FailedLoginCount = 0;
            LockoutUntil = now + lockoutDuration;
        }

        UpdatedAt = now;
    }

    /// <summary>
    /// Zera o contador e libera a conta após login bem-sucedido.
    /// </summary>
    public void ResetFailedLogins()
    {
        FailedLoginCount = 0;
        LockoutUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }
}
