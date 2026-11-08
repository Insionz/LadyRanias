using LadyRanias.App.Components.CatImages.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.CatImages.Controllers;

/// <summary>
/// Forsidebildene. Adressen har en id, som de andre (api/admin/site/{id}/images), så bildeeditoren i admin kan
/// brukes som den er. Bare den faste id-en for forsiden finnes.
/// </summary>
[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/site/{ownerId:guid}/images")]
public sealed class FrontPageImageAdminController(ICatImageHandler handler) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid ownerId, IFormFile formFile, CancellationToken cancellationToken)
    {
        if (ownerId != FrontPageImages.OwnerId) return NotFound();

        using var content = formFile.OpenReadStream();
        try
        {
            var response = await handler.UploadForFrontPageAsync(content, formFile.ContentType, formFile.Length, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpPut("{imageId:guid}")]
    public async Task<ActionResult<CatImageDto>> SetTakenAt(Guid ownerId, Guid imageId, SetTakenAtDto dto, CancellationToken cancellationToken)
    {
        if (ownerId != FrontPageImages.OwnerId) return NotFound();

        try
        {
            var image = await handler.SetTakenAtAsync(ownerId, imageId, dto.TakenAt, cancellationToken);
            return image == null ? NotFound() : image;
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid ownerId, Guid imageId, CancellationToken cancellationToken)
    {
        if (ownerId != FrontPageImages.OwnerId) return NotFound();

        return await handler.DeleteAsync(ownerId, imageId, cancellationToken) ? NoContent() : NotFound();
    }
}
