using EvolFit.Application.Features.Dashboard.DTOs;

namespace EvolFit.Application.Features.Dashboard;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync(CancellationToken ct = default);
    Task<DashboardProgressResponse> GetProgressAsync(int periodDays, CancellationToken ct = default);
    Task<DashboardComplianceResponse> GetComplianceAsync(int days, CancellationToken ct = default);
}
