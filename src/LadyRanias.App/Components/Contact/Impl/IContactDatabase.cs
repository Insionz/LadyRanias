namespace LadyRanias.App.Components.Contact.Impl;

internal interface IContactDatabase
{
    Task SaveAsync(ContactMessageEntity entity, CancellationToken cancellationToken);
    Task<ContactMessageEntity?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContactMessageEntity>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
