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
    private readonly IExerciseSearchService _search;

    public ExercisesController(IWgerExerciseClient wger, IExerciseSearchService search)
    {
        _wger = wger;
        _search = search;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string term,
        [FromQuery] string language = "all",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new { error = "Termo de busca é obrigatório." });

        var result = await _search.SearchAsync(term, language, ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await _wger.GetExerciseInfoAsync(id, ct);
        return Ok(result);
    }
}