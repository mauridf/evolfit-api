using EvolFit.Application.Features.TinyFn.Interfaces;
using Microsoft.Extensions.Options;

namespace EvolFit.Infrastructure.ExternalServices.TinyFn;

/// <summary>
/// Janela fixa por dia (UTC) contando requisições reais à TinyFn (TFN-002).
/// </summary>
public class TinyFnRateLimiter : ITinyFnRateLimiter
{
    private readonly int _maxRequestsPerDay;
    private readonly TimeProvider _time;
    private readonly object _gate = new();
    private DateOnly _currentDay;
    private int _count;

    public TinyFnRateLimiter(IOptions<TinyFnOptions> options)
        : this(options, TimeProvider.System) { }

    public TinyFnRateLimiter(IOptions<TinyFnOptions> options, TimeProvider time)
    {
        _maxRequestsPerDay = Math.Max(1, options.Value.MaxRequestsPerDay);
        _time = time;
    }

    public int MaxRequestsPerDay => _maxRequestsPerDay;

    public int GetRemainingRequests()
    {
        lock (_gate)
        {
            RollWindow();
            return Math.Max(0, _maxRequestsPerDay - _count);
        }
    }

    public bool TryConsumeRequest()
    {
        lock (_gate)
        {
            RollWindow();

            if (_count >= _maxRequestsPerDay) return false;

            _count++;
            return true;
        }
    }

    private void RollWindow()
    {
        var today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);
        if (today == _currentDay) return;

        _currentDay = today;
        _count = 0;
    }
}