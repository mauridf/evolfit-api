using EvolFit.Application.Features.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvolFit.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _service.GetDashboardAsync(ct);
        return Ok(result);
    }

    [HttpGet("progress")]
    public async Task<IActionResult> GetProgress(
        [FromQuery] int period = 90, CancellationToken ct = default)
    {
        var result = await _service.GetProgressAsync(period, ct);
        return Ok(result);
    }

    [HttpGet("compliance")]
    public async Task<IActionResult> GetCompliance(
        [FromQuery] int days = 7, CancellationToken ct = default)
    {
        var result = await _service.GetComplianceAsync(days, ct);
        return Ok(result);
    }
}
