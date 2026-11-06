namespace LadyRanias.App.Components.Litters.Api;

/// <summary>
/// Andre komponenter kan implementere dette for å rydde opp når et kull slettes, på samme måte som ICatDeletedListener.
/// CatImages bruker det til å slette bildene av kullet.
/// </summary>
public interface ILitterDeletedListener
{
    Task OnLitterDeletedAsync(Guid litterId, CancellationToken cancellationToken);
}
