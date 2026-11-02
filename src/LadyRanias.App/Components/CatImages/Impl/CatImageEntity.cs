namespace LadyRanias.App.Components.CatImages.Impl;

internal sealed class CatImageEntity
{
    public required Guid CatId { get; set; }
    public required Guid ImageId { get; set; }
    public required string ContentType { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
