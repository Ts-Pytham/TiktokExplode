using FluentAssertions;
using NSubstitute;
using TiktokExplode.Domain.Enums;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Clients;
using TiktokExplode.Infrastructure.Fetchers;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Tests.Infrastructure.Parsers;

public class TikTokVideoParserTests
{
    private const string ValidUrl = "https://www.tiktok.com/@js_nightwave/video/7579504710961548565";

    private static readonly TikTokOptions NoRetry = new()
    {
        MaxWafRetries = 0,
        RetryBaseDelay = TimeSpan.Zero
    };

    private static TiktokClient CreateClient(string htmlContent)
    {
        var fetcher = Substitute.For<IPageFetcher>();
        fetcher.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(new PageFetchResult { HtmlContent = htmlContent, Cookies = [] });
        return new TiktokClient(fetcher, NoRetry);
    }

    [Fact]
    public async Task Should_MapAllFields_When_HtmlContainsValidVideoData()
    {
        await using var client = CreateClient(TikTokFixtures.ValidVideoHtml);

        var video = await client.GetVideoAsync(ValidUrl);

        // Root fields
        video.Id.Should().Be("7579504710961548565");
        video.Description.Should().Be("Nightwave Plaza session");
        video.Location.Should().Be("US");
        video.CreatedAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1_700_000_000));

        // Language
        video.Language.PrimaryLanguage.Should().Be("en");
        video.Language.IsTranslatable.Should().BeTrue();

        // Author
        video.Author.UniqueId.Should().Be("js_nightwave");
        video.Author.Name.Should().Be("Nightwave Plaza");
        video.Author.Description.Should().Be("Chill music");
        video.Author.IsVerified.Should().BeFalse();
        video.Author.IsPrivate.Should().BeFalse();
        video.Author.Avatar.Larger.Should().Be("https://example.com/avatar_large.jpg");
        video.Author.Avatar.Medium.Should().Be("https://example.com/avatar_medium.jpg");
        video.Author.Avatar.Small.Should().Be("https://example.com/avatar_thumb.jpg");

        // Author stats (values were string-encoded in JSON → numeric string parsing path)
        video.Author.Stats.Followers.Should().Be(1_000);
        video.Author.Stats.Following.Should().Be(500);
        video.Author.Stats.Friends.Should().Be(50);
        video.Author.Stats.LikesReceived.Should().Be(50_000);
        video.Author.Stats.VideoCount.Should().Be(100);

        // Video info
        video.Info.Ratio.Should().Be("720p");
        video.Info.Width.Should().Be(1_080);
        video.Info.Height.Should().Be(1_920);
        video.Info.DownloadLinks.OriginalUrl.Should().Be("https://cdn.example.com/play/video.mp4");
        video.Info.DownloadLinks.WatermarkedUrl.Should().Be("https://cdn.example.com/download/video.mp4");
        video.Info.DownloadLinks.OriginalSizeInBytes.Should().Be(10_485_760);

        // Bitrates
        video.Info.Bitrates.Should().HaveCount(1);
        video.Info.Bitrates[0].Value.Should().Be(1_000_000);
        video.Info.Bitrates[0].FPS.Should().Be(30);
        video.Info.Bitrates[0].Format.Should().Be(BitrateFormat.Mp4);
        video.Info.Bitrates[0].CodecType.Should().Be("h264");

        // Stats (string-encoded in JSON)
        video.Stats.Views.Should().Be(100_000);
        video.Stats.Likes.Should().Be(5_000);
        video.Stats.Comments.Should().Be(200);
        video.Stats.Shares.Should().Be(1_000);
        video.Stats.Favorites.Should().Be(300);
        video.Stats.Reposts.Should().Be(50);

        // Duration
        video.Duration.Seconds.Should().Be(15);
        video.Duration.PreciseSeconds.Should().BeApproximately(15.123, precision: 0.001);

        // Cover
        video.Cover.StaticUrl.Should().Be("https://cdn.example.com/cover_static.jpg");
        video.Cover.AnimatedUrl.Should().Be("https://cdn.example.com/cover_animated.webp");
    }

    [Fact]
    public async Task Should_SetBitrateFormatToUnknown_When_FormatStringIsUnrecognized()
    {
        var html = TikTokFixtures.ValidVideoHtml.Replace(
            "\"Format\": \"mp4\"",
            "\"Format\": \"hevc_custom_xyz\""
        );
        await using var client = CreateClient(html);

        var video = await client.GetVideoAsync(ValidUrl);

        video.Info.Bitrates[0].Format.Should().Be(BitrateFormat.Unknown);
    }

    [Fact]
    public async Task Should_ThrowTiktokParsingException_When_HydrationScriptIsMissing()
    {
        await using var client = CreateClient(TikTokFixtures.NoHydrationScriptHtml);

        var act = async () => await client.GetVideoAsync(ValidUrl);

        await act.Should().ThrowExactlyAsync<TiktokParsingException>();
    }

    [Fact]
    public async Task Should_ThrowVideoNotFoundException_When_ItemStructNodeIsMissing()
    {
        await using var client = CreateClient(TikTokFixtures.MissingItemStructHtml);

        var act = async () => await client.GetVideoAsync(ValidUrl);

        await act.Should().ThrowExactlyAsync<VideoNotFoundException>();
    }

    [Fact]
    public async Task Should_ThrowTiktokParsingException_When_DefaultScopeIsMissing()
    {
        await using var client = CreateClient(TikTokFixtures.MissingDefaultScopeHtml);

        var act = async () => await client.GetVideoAsync(ValidUrl);

        await act.Should().ThrowExactlyAsync<TiktokParsingException>();
    }
}
