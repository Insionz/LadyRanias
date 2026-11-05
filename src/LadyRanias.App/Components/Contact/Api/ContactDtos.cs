using System.ComponentModel.DataAnnotations;

namespace LadyRanias.App.Components.Contact.Api;

public enum ContactStatusEnum
{
    New = 0,
    Read = 1,
    Answered = 2,
}

/// <summary>En melding slik adminsiden ser den.</summary>
public sealed record ContactMessageDto
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public string? Subject { get; set; }
    public required string Message { get; set; }
    public required ContactStatusEnum Status { get; set; }
    public DateTimeOffset? EmailSentAt { get; set; }   // null til varselet er sendt
    public required DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Det den besøkende fyller ut. [ApiController] sjekker attributtene automatisk og svarer 400
/// før controller-metoden i det hele tatt kjører.
/// </summary>
public sealed record CreateContactMessageDto
{
    [Required, StringLength(100)]
    public required string Name { get; set; }

    [Required, EmailAddress, StringLength(200)]
    public required string Email { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? Subject { get; set; }

    [Required, StringLength(4000, MinimumLength = 10)]
    public required string Message { get; set; }

    /// <summary>Honeypot (del 2): skjult i skjemaet. Mennesker lar det stå tomt.</summary>
    [StringLength(200)]
    public string? Website { get; set; }
}
