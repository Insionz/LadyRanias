using System.Collections.Concurrent;

namespace LadyRanias.App.Components.CatImages.Impl;

internal sealed class InMemoryImageStore : IImageStore
{
    private readonly ConcurrentDictionary<string, (byte[] Bytes, string ContentType)> _files = new();

    public async Task SaveAsync(string name, Stream content, string contentType, CancellationToken cancellationToken)
    {
        // En Stream kan bare leses én gang, så vi kopierer innholdet til bytes vi kan ta vare på.
        using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        _files[name] = (copy.ToArray(), contentType);
    }

    public Task<StoredImage?> OpenAsync(string name, CancellationToken cancellationToken)
    {
        if (!_files.TryGetValue(name, out var file))
        {
            return Task.FromResult<StoredImage?>(null);
        }

        // Ny strøm hver gang, så samme bilde kan åpnes flere ganger.
        return Task.FromResult<StoredImage?>(new StoredImage(new MemoryStream(file.Bytes), file.ContentType));
    }

    public Task<bool> DeleteAsync(string name, CancellationToken cancellationToken)
    {
        return Task.FromResult(_files.TryRemove(name, out _));
    }
}
