using LadyRanias.App.Components.Litters.Api;

namespace LadyRanias.App.Components.Litters.Impl;

/// <summary>
/// Lovlige statusendringer for en kattunge. En ren funksjon: ingen database og ingen klokke,
/// så hele tabellen kan testes direkte.
///
///   Fra \ Til   Available  Reserved  Sold  Kept
///   Available       –          ✅       ✅     ✅
///   Reserved        ✅         –        ✅     ❌
///   Sold            ❌         ❌       –      ❌
///   Kept            ✅         ❌       ❌     –
/// </summary>
internal static class KittenStatusRules
{
    public static bool CanChange(KittenStatus from, KittenStatus to) => (from, to) switch
    {
        (KittenStatus.Available, KittenStatus.Reserved) => true,
        (KittenStatus.Available, KittenStatus.Sold) => true,
        (KittenStatus.Available, KittenStatus.Kept) => true,

        (KittenStatus.Reserved, KittenStatus.Available) => true, // reservasjonen falt bort
        (KittenStatus.Reserved, KittenStatus.Sold) => true,

        (KittenStatus.Kept, KittenStatus.Available) => true,     // oppdretter har ombestemt seg

        // Alt annet, også «samme status» og alt fra Sold.
        _ => false,
    };
}
