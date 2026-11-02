namespace LadyRanias.App.Components.CatImages.Api;

public enum CatImageSize
{
    Thumb,
    Large,
}

/// <summary>Selve bildefila, klar til å sendes til nettleseren.</summary>
public sealed record CatImageFile(Stream Content, string ContentType);
