namespace LadyRanias.App.Components.CatImages.Api;

/// <summary>
/// Bildene til forsiden har ingen katt, kull eller innlegg å høre til. De lagres under en fast id i stedet.
/// Frontend bruker den samme id-en (SITE_IMAGES_ID i api.ts), så bildeeditoren virker uten endringer.
/// </summary>
public static class FrontPageImages
{
    public static readonly Guid OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
}
