using LadyRanias.App.Components.CatImages.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.CatImages.Controllers;

/// <summary>Bilder av hele kullet. Samme som for en katt, bare med kullets id.</summary>
[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/litters/{litterId:guid}/images")]
public sealed class LitterImageAdminController(ICatImageHandler handler) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid litterId, IFormFile formFile, CancellationToken cancellationToken)
    {
        using var content = formFile.OpenReadStream();

        try
        {
            var response = await handler.UploadForLitterAsync(litterId, content, formFile.ContentType, formFile.Length, cancellationToken);

            if (response == null) return NotFound();

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpPut("{imageId:guid}")]
    public async Task<ActionResult<CatImageDto>> SetTakenAt(Guid litterId, Guid imageId, SetTakenAtDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var image = await handler.SetTakenAtAsync(litterId, imageId, dto.TakenAt, cancellationToken);

            return image == null ? NotFound() : image;
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid litterId, Guid imageId, CancellationToken cancellationToken)
    {
        var deleted = await handler.DeleteAsync(litterId, imageId, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
