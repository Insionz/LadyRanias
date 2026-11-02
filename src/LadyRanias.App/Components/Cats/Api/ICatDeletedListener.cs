namespace LadyRanias.App.Components.Cats.Api;

/// <summary>
/// Andre komponenter kan implementere dette for å rydde opp når en katt slettes.
/// Cats kjenner bare dette interfacet, ikke hvem som implementerer det.
/// Slik kan CatImages reagere på slettede katter uten at Cats må vite at bilder finnes.
/// </summary>
public interface ICatDeletedListener
{
    Task OnCatDeletedAsync(Guid catId, CancellationToken cancellationToken);
}
