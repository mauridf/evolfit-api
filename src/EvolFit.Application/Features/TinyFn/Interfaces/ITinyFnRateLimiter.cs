namespace EvolFit.Application.Features.TinyFn.Interfaces;

/// <summary>
/// Rate limit interno da TinyFn Health API (TFN-002): protege a cota
/// externa (100 req/mês no plano gratuito) limitando chamadas por dia.
/// </summary>
public interface ITinyFnRateLimiter
{
    int MaxRequestsPerDay { get; }
    int GetRemainingRequests();
    bool TryConsumeRequest();
}