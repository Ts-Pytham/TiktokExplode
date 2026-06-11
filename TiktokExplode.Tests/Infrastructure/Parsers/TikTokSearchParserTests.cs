using FluentAssertions;
using NSubstitute;
using System.Runtime.CompilerServices;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Clients;
using TiktokExplode.Infrastructure.Fetchers.Search;

namespace TiktokExplode.Tests.Infrastructure.Parsers;

public class TikTokSearchParserTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static TiktokSearchClient CreateClient(string jsonContent)
    {
        var fetcher = Substitute.For<ISearchFetcher>();
        fetcher
            .FetchSearchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(YieldResult(new SearchFetchResult { JsonContent = jsonContent, Cookies = [] }));
        return new TiktokSearchClient(fetcher);
    }

    private static async IAsyncEnumerable<SearchFetchResult> YieldResult(
        SearchFetchResult result,
        [EnumeratorCancellation] CancellationToken _ = default)
    {
        await Task.Yield();
        yield return result;
    }

    private static async Task<List<Media>> CollectAsync(IAsyncEnumerable<Media> source)
    {
        var list = new List<Media>();
        await foreach (var item in source)
            list.Add(item);
        return list;
    }

    // ── Type dispatch ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnVideo_When_SearchItemHasNoImagePost()
    {
        await using var client = CreateClient(TikTokFixtures.ValidVideoSearchJson);

        var results = await CollectAsync(client.SearchAsync("test"));

        results.Should().ContainSingle()
               .Which.Should().BeOfType<Video>();
    }

    [Fact]
    public async Task Should_ReturnCarousel_When_SearchItemHasImagePost()
    {
        await using var client = CreateClient(TikTokFixtures.ValidCarouselSearchJson);

        var results = await CollectAsync(client.SearchAsync("test"));

        results.Should().ContainSingle()
               .Which.Should().BeOfType<Carousel>();
    }

    // ── Full field mapping — Video ────────────────────────────────────────────

    [Fact]
    public async Task Should_MapAllVideoFields_When_SearchResultIsVideo()
    {
        await using var client = CreateClient(TikTokFixtures.ValidVideoSearchJson);

        var results = await CollectAsync(client.SearchAsync("test"));
        var video = results.Should().ContainSingle().Which.Should().BeOfType<Video>().Subject;

        // Root fields
        video.Id.Should().Be("1111111111111111111");
        video.Description.Should().Be("Search video description");
        video.Location.Should().Be("MX");
        video.CreatedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1_700_100_000));

        // Language
        video.Language.PrimaryLanguage.Should().Be("es");
        video.Language.IsTranslatable.Should().BeFalse();

        // Author
        video.Author.UniqueId.Should().Be("search_user");
        video.Author.Name.Should().Be("Search User");
        video.Author.Description.Should().Be("Search bio");
        video.Author.IsVerified.Should().BeTrue();
        video.Author.IsPrivate.Should().BeFalse();
        video.Author.Avatar.Larger.Should().Be("https://example.com/sv_avatar_large.jpg");
        video.Author.Avatar.Medium.Should().Be("https://example.com/sv_avatar_medium.jpg");
        video.Author.Avatar.Small.Should().Be("https://example.com/sv_avatar_thumb.jpg");

        // Author stats
        video.Author.Stats.Followers.Should().Be(2_000);
        video.Author.Stats.Following.Should().Be(300);
        video.Author.Stats.Friends.Should().Be(25);
        video.Author.Stats.LikesReceived.Should().Be(75_000);
        video.Author.Stats.VideoCount.Should().Be(50);

        // Stats
        video.Stats.Views.Should().Be(200_000);
        video.Stats.Likes.Should().Be(8_000);
        video.Stats.Comments.Should().Be(500);
        video.Stats.Shares.Should().Be(2_000);
        video.Stats.Favorites.Should().Be(600);
        video.Stats.Reposts.Should().Be(100);

        // Cover
        video.Cover.StaticUrl.Should().Be("https://cdn.example.com/sv_cover.jpg");
        video.Cover.AnimatedUrl.Should().Be("https://cdn.example.com/sv_cover.webp");

        // Video-specific info
        video.Duration.Seconds.Should().Be(20);
        video.Duration.PreciseSeconds.Should().BeApproximately(20.75, precision: 0.001);
        video.Info.Width.Should().Be(1_080);
        video.Info.Height.Should().Be(1_920);
        video.Info.Ratio.Should().Be("1080p");
        video.Info.DownloadLinks.OriginalUrl.Should().Be("https://cdn.example.com/sv_play.mp4");
        video.Info.DownloadLinks.WatermarkedUrl.Should().Be("https://cdn.example.com/sv_download.mp4");
        video.Info.DownloadLinks.OriginalSizeInBytes.Should().Be(5_242_880);

        // Bitrate
        video.Info.Bitrates.Should().ContainSingle();
        video.Info.Bitrates[0].Value.Should().Be(2_000_000);
        video.Info.Bitrates[0].FPS.Should().Be(60);
        video.Info.Bitrates[0].CodecType.Should().Be("h265");

        // Music
        video.Music.Id.Should().Be("7000000000000000001");
        video.Music.Title.Should().Be("Search Track");
        video.Music.AuthorName.Should().Be("Search Artist");
        video.Music.AlbumName.Should().Be("Search Album");
        video.Music.IsCopyrighted.Should().BeTrue();
        video.Music.IsOriginal.Should().BeFalse();
    }

    // ── Full field mapping — Carousel ─────────────────────────────────────────

    [Fact]
    public async Task Should_MapAllCarouselFields_When_SearchResultIsCarousel()
    {
        await using var client = CreateClient(TikTokFixtures.ValidCarouselSearchJson);

        var results = await CollectAsync(client.SearchAsync("slides"));
        var carousel = results.Should().ContainSingle().Which.Should().BeOfType<Carousel>().Subject;

        // Root fields
        carousel.Id.Should().Be("2222222222222222222");
        carousel.Description.Should().Be("Search carousel description");
        carousel.Location.Should().Be("US");
        carousel.CreatedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1_700_200_000));

        // Language
        carousel.Language.PrimaryLanguage.Should().Be("en");
        carousel.Language.IsTranslatable.Should().BeTrue();

        // Author
        carousel.Author.UniqueId.Should().Be("carousel_user");
        carousel.Author.Name.Should().Be("Carousel User");
        carousel.Author.IsVerified.Should().BeFalse();
        carousel.Author.Stats.Followers.Should().Be(3_000);

        // Stats
        carousel.Stats.Views.Should().Be(50_000);
        carousel.Stats.Likes.Should().Be(2_000);
        carousel.Stats.Comments.Should().Be(100);

        // Video cover (sourced from the item's "video" node)
        carousel.Cover.StaticUrl.Should().Be("https://cdn.example.com/cu_video_cover.jpg");
        carousel.Cover.AnimatedUrl.Should().Be("https://cdn.example.com/cu_video_animated.webp");

        // Music
        carousel.Music.Id.Should().Be("6000000000000000001");
        carousel.Music.Title.Should().Be("Carousel Track");
        carousel.Music.IsOriginal.Should().BeTrue();
        carousel.Music.IsCopyrighted.Should().BeFalse();

        // Carousel post
        carousel.Post.Title.Should().Be("Carousel Title");

        // Cover image
        carousel.Post.Cover.Width.Should().Be(1_080u);
        carousel.Post.Cover.Height.Should().Be(1_920u);
        carousel.Post.Cover.Urls.Should().ContainSingle()
            .Which.Should().Be("https://cdn.example.com/carousel_cover.jpg");

        // Images collection
        carousel.Post.Images.Should().HaveCount(2);

        var first = carousel.Post.Images[0];
        first.Width.Should().Be(1_080u);
        first.Height.Should().Be(1_920u);
        first.Urls.Should().ContainSingle()
            .Which.Should().Be("https://cdn.example.com/slide1.jpg");

        var second = carousel.Post.Images[1];
        second.Width.Should().Be(720u);
        second.Height.Should().Be(1_280u);
        second.Urls.Should().HaveCount(2);
        second.Urls.Should().Contain("https://cdn.example.com/slide2.jpg");
        second.Urls.Should().Contain("https://cdn.example.com/slide2_fallback.jpg");
    }

    // ── Filtering ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_SkipEntry_When_TypeIsNotOne()
    {
        await using var client = CreateClient(TikTokFixtures.SearchJsonWithNonTypeOneEntry);

        var results = await CollectAsync(client.SearchAsync("test"));

        results.Should().ContainSingle("only the type=1 entry should be returned");
        results[0].Id.Should().Be("1111111111111111111");
    }

    [Fact]
    public async Task Should_ReturnBothTypes_When_DataHasMixedVideoAndCarousel()
    {
        await using var client = CreateClient(TikTokFixtures.MixedVideoAndCarouselSearchJson);

        var results = await CollectAsync(client.SearchAsync("mixed"));

        results.Should().HaveCount(2);
        results.Should().ContainSingle(m => m is Video);
        results.Should().ContainSingle(m => m is Carousel);
    }

    // ── Empty / missing data ──────────────────────────────────────────────────

    [Fact]
    public async Task Should_ReturnEmptySequence_When_DataArrayIsEmpty()
    {
        await using var client = CreateClient(TikTokFixtures.EmptyDataSearchJson);

        var results = await CollectAsync(client.SearchAsync("test"));

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_ReturnEmptySequence_When_DataNodeIsMissing()
    {
        await using var client = CreateClient(TikTokFixtures.MissingDataSearchJson);

        var results = await CollectAsync(client.SearchAsync("test"));

        results.Should().BeEmpty();
    }

    // ── Error cases ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ThrowTiktokParsingException_When_ItemNodeIsMissing()
    {
        await using var client = CreateClient(TikTokFixtures.MissingItemSearchJson);

        var act = async () => await CollectAsync(client.SearchAsync("test"));

        await act.Should().ThrowAsync<TiktokParsingException>();
    }

    [Fact]
    public async Task Should_ThrowTiktokParsingException_When_JsonRootIsNull()
    {
        // JsonNode.Parse("null") returns null → the parser wraps it as TiktokParsingException.
        await using var client = CreateClient("null");

        var act = async () => await CollectAsync(client.SearchAsync("test"));

        await act.Should().ThrowAsync<TiktokParsingException>();
    }
}
