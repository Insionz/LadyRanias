namespace LadyRanias.App.Components.Contact.Api;

public interface IContactHandler
{
    /// <summary>Lagrer meldingen. Varselet på e-post sendes senere, i bakgrunnen (del 3).</summary>
    Task CreateAsync(CreateContactMessageDto message, CancellationToken cancellationToken);
}
