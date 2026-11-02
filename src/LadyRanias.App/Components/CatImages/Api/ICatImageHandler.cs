namespace LadyRanias.App.Components.CatImages.Api;

public interface ICatImageHandler
{
    Task<CatImageDto?> UploadAsync(Guid catId, Stream content, string contentType, long length, CancellationToken cancellationToken);
}
