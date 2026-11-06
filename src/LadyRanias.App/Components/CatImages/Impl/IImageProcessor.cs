namespace LadyRanias.App.Components.CatImages.Impl;

/// <summary>Gjør et opplastet bilde klart for nettsiden: riktig rotasjon, uten metadata, i to størrelser.</summary>
internal interface IImageProcessor
{
    /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException">Hvis fila ikke er et gyldig bilde.</exception>
    Task<ProcessedImage> ProcessAsync(Stream input, CancellationToken cancellationToken);
}

/// <param name="TakenAt">Datoen bildet ble tatt, fra EXIF, hvis bildet hadde den. Metadataen er fjernet fra selve bildene.</param>
internal sealed record ProcessedImage(byte[] Thumbnail, byte[] Large, string ContentType, DateOnly? TakenAt = null);
