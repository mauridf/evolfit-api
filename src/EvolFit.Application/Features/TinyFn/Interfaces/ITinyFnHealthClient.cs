using EvolFit.Application.Features.TinyFn.DTOs;

namespace EvolFit.Application.Features.TinyFn.Interfaces;

/// <summary>
/// Port para a TinyFn Health API. Implementada na Infrastructure.
/// </summary>
public interface ITinyFnHealthClient
{
    Task<BmiResponse> CalculateBmiAsync(BmiRequest request, CancellationToken ct = default);
    Task<BmrResponse> CalculateBmrAsync(BmrRequest request, CancellationToken ct = default);
    Task<TdeeResponse> CalculateTdeeAsync(TdeeRequest request, CancellationToken ct = default);
    Task<CaloriesResponse> CalculateCaloriesAsync(CaloriesRequest request, CancellationToken ct = default);
}
