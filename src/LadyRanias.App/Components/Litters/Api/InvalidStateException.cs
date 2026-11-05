namespace LadyRanias.App.Components.Litters.Api;

/// <summary>
/// Forespørselen er gyldig i seg selv, men passer ikke med tilstanden akkurat nå.
/// For eksempel å selge en kattunge som allerede er solgt, eller markere et kull som født to ganger.
/// Controlleren gjør dette om til 409 Conflict. Ugyldig input (ValidationException) blir 400 Bad Request.
/// </summary>
public sealed class InvalidStateException(string message) : Exception(message);
