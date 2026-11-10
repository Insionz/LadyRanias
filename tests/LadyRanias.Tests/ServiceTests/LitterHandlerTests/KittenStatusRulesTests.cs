using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Litters.Impl;

namespace LadyRanias.Tests.ServiceTests.LitterHandlerTests;

[TestClass]
public sealed class KittenStatusRulesTests
{
    // Hele overgangstabellen fra KittenStatusRules, rad for rad. 5 × 5 = 25 kombinasjoner.
    [TestMethod]
    [DataRow(KittenStatus.Available, KittenStatus.Available, false)]
    [DataRow(KittenStatus.Available, KittenStatus.Reserved, true)]
    [DataRow(KittenStatus.Available, KittenStatus.Sold, true)]
    [DataRow(KittenStatus.Available, KittenStatus.Kept, true)]
    [DataRow(KittenStatus.Available, KittenStatus.UnderEvaluation, true)]

    [DataRow(KittenStatus.Reserved, KittenStatus.Available, true)]
    [DataRow(KittenStatus.Reserved, KittenStatus.Reserved, false)]
    [DataRow(KittenStatus.Reserved, KittenStatus.Sold, true)]
    [DataRow(KittenStatus.Reserved, KittenStatus.Kept, false)]
    [DataRow(KittenStatus.Reserved, KittenStatus.UnderEvaluation, false)]

    [DataRow(KittenStatus.Sold, KittenStatus.Available, true)]
    [DataRow(KittenStatus.Sold, KittenStatus.Reserved, false)]
    [DataRow(KittenStatus.Sold, KittenStatus.Sold, false)]
    [DataRow(KittenStatus.Sold, KittenStatus.Kept, true)]
    [DataRow(KittenStatus.Sold, KittenStatus.UnderEvaluation, false)]

    [DataRow(KittenStatus.Kept, KittenStatus.Available, true)]
    [DataRow(KittenStatus.Kept, KittenStatus.Reserved, false)]
    [DataRow(KittenStatus.Kept, KittenStatus.Sold, false)]
    [DataRow(KittenStatus.Kept, KittenStatus.Kept, false)]
    [DataRow(KittenStatus.Kept, KittenStatus.UnderEvaluation, true)]

    [DataRow(KittenStatus.UnderEvaluation, KittenStatus.Available, true)]
    [DataRow(KittenStatus.UnderEvaluation, KittenStatus.Reserved, false)]
    [DataRow(KittenStatus.UnderEvaluation, KittenStatus.Sold, false)]
    [DataRow(KittenStatus.UnderEvaluation, KittenStatus.Kept, true)]
    [DataRow(KittenStatus.UnderEvaluation, KittenStatus.UnderEvaluation, false)]
    public void CanChange_follows_the_table(KittenStatus from, KittenStatus to, bool expected)
    {
        Assert.AreEqual(expected, KittenStatusRules.CanChange(from, to));
    }
}
