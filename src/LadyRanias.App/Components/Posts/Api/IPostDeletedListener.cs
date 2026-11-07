namespace LadyRanias.App.Components.Posts.Api;

/// <summary>Som ICatDeletedListener og ILitterDeletedListener: CatImages sletter bildene til et innlegg som slettes.</summary>
public interface IPostDeletedListener
{
    Task OnPostDeletedAsync(Guid postId, CancellationToken cancellationToken);
}
