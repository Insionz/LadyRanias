using LadyRanias.App.Components.Cats.Api;

namespace LadyRanias.App.Components.Litters.Impl;

/// <summary>
/// Lovlige statusendringer for en kattunge. En ren funksjon: ingen database og ingen klokke,
/// så hele tabellen kan testes direkte.
///
///   Fra \ Til         Available  Reserved  Sold  Kept  UnderEvaluation
///   Available             –          ✅       ✅     ✅        ✅
///   Reserved              ✅         –        ✅     ❌        ❌
///   Sold                  ✅         ❌       –      ✅        ❌
///   Kept                  ✅         ❌       ❌     –         ✅
///   UnderEvaluation       ✅         ❌       ❌     ✅        –
///
/// «Under evaluering»: oppdretteren vurderer om hun skal beholde kattungen. Derfra blir den enten ledig eller beholdt.
/// Den kan ikke reserveres eller selges direkte, for da er den ikke ferdig vurdert.
///
/// «Solgt» kan angres: salget gikk tilbake (ledig igjen), eller vi beholder den likevel. Også når den ble satt til solgt ved en feil.
/// </summary>
internal static class KittenStatusRules
{
    public static bool CanChange(KittenStatus from, KittenStatus to) => (from, to) switch
    {
        (KittenStatus.Available, KittenStatus.Reserved) => true,
        (KittenStatus.Available, KittenStatus.Sold) => true,
        (KittenStatus.Available, KittenStatus.Kept) => true,
        (KittenStatus.Available, KittenStatus.UnderEvaluation) => true,

        (KittenStatus.Reserved, KittenStatus.Available) => true, // reservasjonen falt bort
        (KittenStatus.Reserved, KittenStatus.Sold) => true,

        (KittenStatus.Sold, KittenStatus.Available) => true,     // salget gikk tilbake
        (KittenStatus.Sold, KittenStatus.Kept) => true,          // vi beholder den likevel

        (KittenStatus.Kept, KittenStatus.Available) => true,     // oppdretter har ombestemt seg
        (KittenStatus.Kept, KittenStatus.UnderEvaluation) => true,

        (KittenStatus.UnderEvaluation, KittenStatus.Available) => true, // skal selges
        (KittenStatus.UnderEvaluation, KittenStatus.Kept) => true,      // blir hos oss

        // Alt annet, også «samme status».
        _ => false,
    };
}
