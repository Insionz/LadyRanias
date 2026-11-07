using LadyRanias.App.Components.Cats.Api;
using LadyRanias.App.Components.Cats.Impl;
using LadyRanias.App.Components.Posts.Api;
using LadyRanias.App.Components.Posts.Impl;
using Microsoft.Extensions.Time.Testing;
using System.ComponentModel.DataAnnotations;

namespace LadyRanias.Tests.ServiceTests.PostHandlerTests;

[TestClass]
public sealed class PostHandlerTests
{
    private static readonly DateTimeOffset Start = new(2026, 11, 7, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _clock = new(Start);
    private readonly InMemoryPostDatabase _database = new();
    private readonly CatHandler _cats;
    private readonly PostHandler _handler;
    private CatDto _kiara = null!;

    public PostHandlerTests()
    {
        // Ekte CatHandler, med lytteren som fjerner slettede katter fra innleggene, slik det skjer i appen.
        _cats = new CatHandler(new InMemoryCatDatabase(), _clock, [new RemoveCatFromPostsWhenCatIsDeleted(_database)]);
        _handler = new PostHandler(_database, _cats, _clock, []);
    }

    [TestInitialize]
    public async Task CreateCatAsync()
    {
        _kiara = await _cats.CreateAsync(new CreateCatDto
        {
            Breed = Breed.MaineCoon,
            Name = "Kiara",
            Sex = SexEnum.Female,
            BirthDate = new DateOnly(2022, 1, 1),
        }, CancellationToken.None);
    }

    private static SavePostDto NewPost(string title = "Utstilling i Hamar", bool published = true) => new()
    {
        Title = title,
        Body = "Kiara ble Best in Show!",
        Date = new DateOnly(2026, 10, 3),
        Kind = PostKind.Show,
        Published = published,
    };

    [TestMethod]
    public async Task Draft_is_only_visible_to_admin()
    {
        var draft = await _handler.CreateAsync(NewPost(published: false), CancellationToken.None);

        Assert.HasCount(0, await _handler.GetPublishedAsync(null, CancellationToken.None));
        Assert.IsNull(await _handler.GetAsync(draft.Id, includeDrafts: false, CancellationToken.None));
        Assert.IsNotNull(await _handler.GetAsync(draft.Id, includeDrafts: true, CancellationToken.None));
        Assert.HasCount(1, await _handler.GetAllAsync(CancellationToken.None));
    }

    [TestMethod]
    public async Task Posts_are_sorted_by_the_date_it_happened_newest_first()
    {
        var october = await _handler.CreateAsync(NewPost("Oktober") with { Date = new DateOnly(2026, 10, 3) }, CancellationToken.None);
        _clock.Advance(TimeSpan.FromMinutes(1));
        // Skrevet senere, men handler om noe som skjedde før.
        var march = await _handler.CreateAsync(NewPost("Mars") with { Date = new DateOnly(2026, 3, 1) }, CancellationToken.None);

        var posts = await _handler.GetPublishedAsync(null, CancellationToken.None);

        Assert.AreEqual(october.Id, posts[0].Id);
        Assert.AreEqual(march.Id, posts[1].Id);
    }

    [TestMethod]
    public async Task Posts_about_a_cat_can_be_found_from_the_cat()
    {
        var aboutKiara = await _handler.CreateAsync(NewPost() with { CatIds = [_kiara.Id, _kiara.Id] }, CancellationToken.None);
        await _handler.CreateAsync(NewPost("Noe annet"), CancellationToken.None);

        var posts = await _handler.GetPublishedAsync(_kiara.Id, CancellationToken.None);

        Assert.HasCount(1, posts);
        Assert.AreEqual(aboutKiara.Id, posts[0].Id);
        Assert.HasCount(1, posts[0].CatIds); // duplikater fjernes
    }

    [TestMethod]
    public async Task Unknown_cat_or_empty_title_is_rejected()
    {
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(NewPost() with { CatIds = [Guid.NewGuid()] }, CancellationToken.None));
        await Assert.ThrowsExactlyAsync<ValidationException>(
            () => _handler.CreateAsync(NewPost("   "), CancellationToken.None));
    }

    [TestMethod]
    public async Task Deleting_a_cat_removes_it_from_posts_but_keeps_the_post()
    {
        var post = await _handler.CreateAsync(NewPost() with { CatIds = [_kiara.Id] }, CancellationToken.None);

        await _cats.DeleteAsync(_kiara.Id, CancellationToken.None);

        var fetched = await _handler.GetAsync(post.Id, includeDrafts: true, CancellationToken.None);
        Assert.IsNotNull(fetched);
        Assert.HasCount(0, fetched.CatIds);
    }

    [TestMethod]
    public async Task Update_changes_fields_but_keeps_created_at()
    {
        var post = await _handler.CreateAsync(NewPost(published: false), CancellationToken.None);
        _clock.Advance(TimeSpan.FromDays(1));

        var updated = await _handler.UpdateAsync(post.Id, NewPost("Ny tittel", published: true), CancellationToken.None);

        Assert.IsNotNull(updated);
        Assert.AreEqual("Ny tittel", updated.Title);
        Assert.IsTrue(updated.Published);
        Assert.AreEqual(Start, updated.CreatedAt);
        Assert.IsNull(await _handler.UpdateAsync(Guid.NewGuid(), NewPost(), CancellationToken.None));
    }
}
