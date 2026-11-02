namespace LadyRanias.App.Components.CatImages.Impl;

internal interface IImageStore
{
    Task SaveAsync(string name, Stream content, string contentType, CancellationToken cancellationToken);
    Task<StoredImage?> OpenAsync(string name, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string name, CancellationToken cancellationToken);
}
internal sealed record StoredImage(Stream Content, string ContentType);