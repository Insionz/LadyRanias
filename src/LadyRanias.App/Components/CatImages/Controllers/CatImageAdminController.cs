using LadyRanias.App.Components.CatImages.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.CatImages.Controllers;

[ApiController]
[Authorize(Policy = "Admin")]
[Route("api/admin/cats")]
public sealed class CatImageAdminController(ICatImageHandler handler) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [Route("{catId:guid}/images")]
    public async Task<IActionResult> Upload(Guid catId, IFormFile formFile, CancellationToken cancellationToken)
    {
        using var content = formFile.OpenReadStream();

        try
        {
            var response = await handler.UploadAsync(catId, content, formFile.ContentType, formFile.Length, cancellationToken);

            if (response == null) return NotFound();

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ValidationException ex)
        {
            return ValidationProblem(detail: ex.Message);
        }
    }

    [HttpDelete("{catId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid catId, Guid imageId, CancellationToken cancellationToken)
    {
        var deleted = await handler.DeleteAsync(catId, imageId, cancellationToken);

        return deleted ? NoContent() : NotFound();
    }
}
