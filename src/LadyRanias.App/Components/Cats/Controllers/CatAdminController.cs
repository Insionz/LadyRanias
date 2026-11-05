using LadyRanias.App.Components.Cats.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Cats.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/cats")]
public sealed class CatAdminController(ICatHandler handler) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<CatDto>> GetAll(CancellationToken ct)
    {
        var allCats = handler.GetAllAsync(ct);
        return allCats;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CatDto>> Get(Guid id, CancellationToken ct)
    {
        var cat = await handler.GetAsync(id, ct);
        
        if (cat == null)
        {
            return NotFound();
        }

        return cat;
    }

    [HttpPost]
    public async Task<ActionResult<CatDto>> Create(CreateCatDto createCatDto, CancellationToken ct)
    {
        try
        {
            var createdCat = await handler.CreateAsync(createCatDto, ct);

            return CreatedAtAction(nameof(Get), new { id = createdCat.Id }, createdCat);

        }
        catch (ValidationException ex) 
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CatDto>> Update(Guid id, UpdateCatDto updateCatDto, CancellationToken ct)
    {
        try
        {
            var updatedCat = await handler.UpdateAsync(id, updateCatDto, ct);

            if (updatedCat == null)
            {
                return NotFound();
            }

            return updatedCat;
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await handler.DeleteAsync(id, ct);

        return deleted ? NoContent() : NotFound();
    }
}
