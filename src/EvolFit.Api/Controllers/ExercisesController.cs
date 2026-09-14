using EvolFit.Application.Features.Wger.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvolFit.Api.Controllers;

[ApiController]
[Route("api/exercises")]
[Authorize]
public class ExercisesController : ControllerBase
{
    private readonly IWgerExerciseClient _wger;

    public ExercisesController(IWgerExerciseClient wger)
    {
        _wger = wger;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string term,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new { error = "Termo de busca é obrigatório." });

        var result = await _wger.SearchExercisesAsync(term, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _wger.GetExerciseInfoAsync(id, ct);
        return Ok(result);
    }
}
