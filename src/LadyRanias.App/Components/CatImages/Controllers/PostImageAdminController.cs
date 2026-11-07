using LadyRanias.App.Components.CatImages.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.CatImages.Controllers;

/// <summary>Bilder til et innlegg i nyhetene. Samme som for en katt, bare med innleggets id.</summary>
[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/posts/{postId:guid}/images")]
public sealed class PostImageAdminController(ICatImageHandler handler) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(Guid postId, IFormFile formFile, CancellationToken cancellationToken)
    {
        using var content = formFile.OpenReadStream();

        try
        {
            var response = await handler.UploadForPostAsync(postId, content, formFile.ContentType, formFile.Length, cancellationToken);

            if (response == null) return NotFound();

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpPut("{imageId:guid}")]
    public async Task<ActionResult<CatImageDto>> SetTakenAt(Guid postId, Guid imageId, SetTakenAtDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var image = await handler.SetTakenAtAsync(postId, imageId, dto.TakenAt, cancellationToken);

            return image == null ? NotFound() : image;
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid postId, Guid imageId, CancellationToken cancellationToken)
    {
        var deleted = await handler.DeleteAsync(postId, imageId, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
