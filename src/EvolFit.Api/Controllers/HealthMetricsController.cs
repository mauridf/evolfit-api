using EvolFit.Application.Features.Health;
using EvolFit.Application.Features.Health.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvolFit.Api.Controllers;

[ApiController]
[Route("api/health/metrics")]
[Authorize]
public class HealthMetricsController : ControllerBase
{
    private readonly IHealthService _service;

    public HealthMetricsController(IHealthService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(HealthMetricResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHealthMetricRequest request, CancellationToken ct)
    {
        var response = await _service.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<HealthMetricListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(HealthMetricResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatest(CancellationToken ct)
    {
        var result = await _service.GetLatestAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(HealthMetricResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpGet("evolution")]
    [ProducesResponseType(typeof(EvolutionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvolution(
        [FromQuery] int period = 90, CancellationToken ct = default)
    {
        var result = await _service.GetEvolutionAsync(period, ct);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
