using LadyRanias.App.Components.Litters.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Litters.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/litters")]
public sealed class LitterAdminController(ILitterHandler handler) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LitterDto>> Get(Guid id, CancellationToken ct)
    {
        var litter = await handler.GetAsync(id, ct);

        return litter == null ? NotFound() : litter;
    }

    [HttpPost]
    public Task<ActionResult<LitterDto>> Create(CreateLitterDto newLitter, CancellationToken ct)
    {
        return Run<LitterDto>(async () =>
        {
            var litter = await handler.CreateAsync(newLitter, ct);
            return CreatedAtAction(nameof(Get), new { id = litter.Id }, litter);
        });
    }

    [HttpPost("{id:guid}/born")]
    public Task<ActionResult<LitterDto>> MarkBorn(Guid id, MarkBornDto born, CancellationToken ct)
    {
        return Run<LitterDto>(async () =>
        {
            var litter = await handler.MarkBornAsync(id, born, ct);
            return litter == null ? NotFound() : litter;
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        return await handler.DeleteAsync(id, ct) ? NoContent() : NotFound();
    }

    [HttpPost("{litterId:guid}/kittens")]
    public Task<ActionResult<KittenDto>> AddKitten(Guid litterId, CreateKittenDto newKitten, CancellationToken ct)
    {
        return Run<KittenDto>(async () =>
        {
            var kitten = await handler.AddKittenAsync(litterId, newKitten, ct);
            return kitten == null ? NotFound() : StatusCode(StatusCodes.Status201Created, kitten);
        });
    }

    [HttpPut("{litterId:guid}/kittens/{kittenId:guid}/status")]
    public Task<ActionResult<KittenDto>> ChangeKittenStatus(Guid litterId, Guid kittenId, ChangeKittenStatusDto change, CancellationToken ct)
    {
        return Run<KittenDto>(async () =>
        {
            var kitten = await handler.ChangeKittenStatusAsync(litterId, kittenId, change, ct);
            return kitten == null ? NotFound() : kitten;
        });
    }

    [HttpDelete("{litterId:guid}/kittens/{kittenId:guid}")]
    public async Task<IActionResult> DeleteKitten(Guid litterId, Guid kittenId, CancellationToken ct)
    {
        try
        {
            return await handler.DeleteKittenAsync(litterId, kittenId, ct) ? NoContent() : NotFound();
        }
        catch (InvalidStateException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>Kobler en katt vi allerede har (en beholdt kattunge som nå er voksen), til kullet den er født i.</summary>
    [HttpPost("{litterId:guid}/kittens/link")]
    public Task<ActionResult<KittenDto>> LinkExistingCat(Guid litterId, LinkCatDto link, CancellationToken ct)
    {
        return Run<KittenDto>(async () =>
        {
            var kitten = await handler.LinkExistingCatAsync(litterId, link, ct);
            return kitten == null ? NotFound() : kitten;
        });
    }

    [HttpDelete("{litterId:guid}/kittens/{catId:guid}/link")]
    public async Task<IActionResult> UnlinkCat(Guid litterId, Guid catId, CancellationToken ct)
    {
        try
        {
            return await handler.UnlinkCatAsync(litterId, catId, ct) ? NoContent() : NotFound();
        }
        catch (InvalidStateException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>
    /// Gjør om feil fra handleren til riktig HTTP-svar, på ett sted i stedet for en try/catch i hver metode:
    /// ValidationException blir 400 Bad Request, og InvalidStateException blir 409 Conflict.
    /// </summary>
    private async Task<ActionResult<T>> Run<T>(Func<Task<ActionResult<T>>> action)
    {
        try
        {
            return await action();
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
        catch (InvalidStateException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
