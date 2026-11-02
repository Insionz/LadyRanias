namespace LadyRanias.App.Components.CatImages.Api;

public class CatImageDto
{
    public required Guid ImageId { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}