namespace LadyRanias.App.Components.CatImages.Impl;

internal sealed class CatImageEntity
{
    /// <summary>Eieren: katten, eller kullet for bilder av hele kullet. Navnet er fra før kullene fikk bilder.</summary>
    public required Guid CatId { get; set; }
    public required Guid ImageId { get; set; }
    public required string ContentType { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>Når bildet ble tatt. null for bilder lastet opp før dette fantes: da brukes opplastingsdagen.</summary>
    public DateOnly? TakenAt { get; set; }
}
