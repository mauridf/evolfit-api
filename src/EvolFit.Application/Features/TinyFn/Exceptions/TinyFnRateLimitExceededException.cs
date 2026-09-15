namespace EvolFit.Application.Features.TinyFn.Exceptions;

/// <summary>
/// Cota diária de chamadas à TinyFn atingida (TFN-002) — sinaliza o
/// fallback local (TFN-005) sem quebrar o fluxo.
/// </summary>
public sealed class TinyFnRateLimitExceededException : Exception
{
    public TinyFnRateLimitExceededException()
        : base("Limite diário de requisições à TinyFn atingido (TFN-002).") { }
}