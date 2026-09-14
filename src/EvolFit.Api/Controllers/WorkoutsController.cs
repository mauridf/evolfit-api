using EvolFit.Application.Features.Workouts;
using EvolFit.Application.Features.Workouts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvolFit.Api.Controllers;

[ApiController]
[Route("api/workouts")]
[Authorize]
public class WorkoutsController : ControllerBase
{
    private readonly IWorkoutService _service;

    public WorkoutsController(IWorkoutService service)
    {
        _service = service;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(WorkoutRoutineGenerateResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Generate(
        [FromBody] GenerateWorkoutRequest request, CancellationToken ct)
    {
        var response = await _service.GenerateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? status = null,
        CancellationToken ct = default)
    {
        var result = await _service.ListAsync(page, pageSize, status, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(result);
    }

    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id, [FromBody] UpdateWorkoutStatusRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateStatusAsync(id, request, ct);
        return Ok(result);
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetToday(CancellationToken ct)
    {
        var result = await _service.GetTodayAsync(ct);
        return Ok(result);
    }

    [HttpPost("log")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> LogExercise(
        [FromBody] LogExerciseRequest request, CancellationToken ct)
    {
        await _service.LogExerciseAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("{id:int}/progress")]
    public async Task<IActionResult> GetProgress(int id, CancellationToken ct)
    {
        var result = await _service.GetProgressAsync(id, ct);
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
