namespace LadyRanias.App.Components.CatImages.Api;

public class CatImageDto
{
    public required Guid ImageId { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Når bildet ble tatt: fra kameraet (EXIF), ellers opplastingsdagen, og mamma kan rette den.
    /// Siden regner ut «uke 5» fra denne og fødselsdatoen.
    /// </summary>
    public required DateOnly TakenAt { get; set; }
}

public sealed record SetTakenAtDto
{
    public required DateOnly TakenAt { get; set; }
}
