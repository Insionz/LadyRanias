using LadyRanias.App.Components.Contact.Api;

namespace LadyRanias.App.Components.Contact.Impl;

internal sealed record ContactMessageEntity
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public required string Message { get; set; }
    public required ContactStatusEnum Status { get; set; }
    public DateTimeOffset? EmailSentAt { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
