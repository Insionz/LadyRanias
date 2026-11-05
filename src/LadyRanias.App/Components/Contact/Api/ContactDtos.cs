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
/// før controller-metoden i det hele tatt kjører. Feilmeldingene er på norsk, fordi de vises i skjemaet.
/// </summary>
public sealed record CreateContactMessageDto
{
    [Required(ErrorMessage = "Skriv inn navnet ditt.")]
    [StringLength(100, ErrorMessage = "Navnet kan være maks 100 tegn.")]
    public required string Name { get; set; }

    [Required(ErrorMessage = "Skriv inn e-postadressen din.")]
    [EmailAddress(ErrorMessage = "E-postadressen ser ikke riktig ut.")]
    [StringLength(200, ErrorMessage = "E-postadressen kan være maks 200 tegn.")]
    public required string Email { get; set; }

    [StringLength(30, ErrorMessage = "Telefonnummeret kan være maks 30 tegn.")]
    public string? Phone { get; set; }

    [StringLength(200, ErrorMessage = "Emnet kan være maks 200 tegn.")]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Skriv en melding.")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Meldingen må være mellom 10 og 4000 tegn.")]
    public required string Message { get; set; }

    /// <summary>Honeypot: skjult i skjemaet. Mennesker lar det stå tomt.</summary>
    [StringLength(200)]
    public string? Website { get; set; }
}

public sealed record ChangeContactStatusDto
{
    public required ContactStatusEnum Status { get; set; }
}
