namespace EvolFit.Application.Common;

/// <summary>
/// Fornece o ID do usuário autenticado atual.
/// Garante ARQ-001 (filtro user_id em 100% das queries) de forma centralizada.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Retorna o UserId do JWT. Lança se não autenticado.</summary>
    int UserId { get; }

    /// <summary>Indica se há usuário autenticado no contexto.</summary>
    bool IsAuthenticated { get; }
}
