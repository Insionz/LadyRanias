namespace LadyRanias.App.Components.Cats.Api;

public interface ICatHandler
{
    /// <summary>Alle katter som ikke er kattunger, også eksterne (admin). Kattungene hentes per kull med GetByLitterAsync.</summary>
    Task<IReadOnlyList<CatDto>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Våre egne katter som lever: i avl og pensjonerte. Det den offentlige siden viser.</summary>
    Task<IReadOnlyList<CatDto>> GetOwnAsync(CancellationToken cancellationToken);

    /// <summary>Våre egne katter som er døde, sist døde først. Til minnesiden.</summary>
    Task<IReadOnlyList<CatDto>> GetInMemoryAsync(CancellationToken cancellationToken);

    /// <summary>Henter alle katter, også kattunger.</summary>
    Task<CatDto?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<CatDto> CreateAsync(CreateCatDto entity, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    /// <summary>null hvis katten ikke finnes. ValidationException ved ugyldige verdier.</summary>
    Task<CatDto?> UpdateAsync(Guid id, UpdateCatDto update, CancellationToken cancellationToken);

    // ---- Kattunger. Brukes av Litters, som vet hva et kull er. Cats kjenner bare LitterId. ----

    /// <summary>Kattungene i et kull, eldste først (i den rekkefølgen de ble lagt inn).</summary>
    Task<IReadOnlyList<CatDto>> GetByLitterAsync(Guid litterId, CancellationToken cancellationToken);

    /// <summary>Lager en kattunge: en vanlig katt med Role = Kitten, LitterId og SaleStatus = Available. Role i DTO-en brukes ikke.</summary>
    Task<CatDto> CreateKittenAsync(Guid litterId, CreateCatDto newKitten, CancellationToken cancellationToken);

    /// <summary>
    /// null hvis katten ikke finnes. ValidationException hvis den ikke er en kattunge.
    /// Sjekker ikke om endringen er lov. Det gjør Litters før den kaller denne.
    /// </summary>
    Task<CatDto?> SetSaleStatusAsync(Guid id, KittenStatus status, CancellationToken cancellationToken);
}
