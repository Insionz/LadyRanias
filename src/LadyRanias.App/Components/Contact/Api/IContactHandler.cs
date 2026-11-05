namespace LadyRanias.App.Components.Contact.Api;

public interface IContactHandler
{
    /// <summary>Lagrer meldingen og legger den i køen. E-postvarselet sendes av bakgrunnsjobben.</summary>
    Task CreateAsync(CreateContactMessageDto message, CancellationToken cancellationToken);

    /// <summary>Alle meldinger, nyeste først. Til adminsiden.</summary>
    Task<IReadOnlyList<ContactMessageDto>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>null hvis meldingen ikke finnes.</summary>
    Task<ContactMessageDto?> ChangeStatusAsync(Guid id, ContactStatusEnum status, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
