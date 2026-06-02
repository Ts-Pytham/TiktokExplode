using FluentAssertions;
using NSubstitute;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Infrastructure.Clients;
using TiktokExplode.Infrastructure.Fetchers;
using TiktokExplode.Infrastructure.Options;

namespace TiktokExplode.Tests.Infrastructure.Clients;

public class TiktokClientTests
{
    private const string ValidUrl = "https://www.tiktok.com/@js_nightwave/video/7579504710961548565";

    // ── URL validation ────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_ThrowArgumentException_When_UrlIsInvalid()
    {
        var fetcher = Substitute.For<IPageFetcher>();
        await using var client = new TiktokClient(fetcher, new TiktokOptions());

        var act = async () => await client.GetVideoAsync("not-a-tiktok-url");

        await act.Should().ThrowExactlyAsync<ArgumentException>();
        await fetcher.DidNotReceive().FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── WAF retry ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Should_RetryAndSucceed_When_FetcherThrowsWafOnFirstAttemptOnly()
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxWafRetries = 2, RetryBaseDelay = TimeSpan.Zero };

        fetcher.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(
                   Task.FromException<PageFetchResult>(new TiktokWafException()),
                   Task.FromResult(TikTokFixtures.ValidVideoResult)
               );

        await using var client = new TiktokClient(fetcher, options);

        var video = await client.GetVideoAsync(ValidUrl);

        video.Should().NotBeNull();
        await fetcher.Received(2).FetchPageAsync(ValidUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ThrowTiktokWafException_When_AllRetriesAreExhausted()
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxWafRetries = 2, RetryBaseDelay = TimeSpan.Zero };

        fetcher.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromException<PageFetchResult>(new TiktokWafException()));

        await using var client = new TiktokClient(fetcher, options);

        var act = async () => await client.GetVideoAsync(ValidUrl);

        await act.Should().ThrowExactlyAsync<TiktokWafException>();
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(3, 4)]
    public async Task Should_CallFetcherExactlyMaxRetriesPlusOneTimes_When_WafAlwaysThrows(
        int maxWafRetries,
        int expectedCalls)
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxWafRetries = maxWafRetries, RetryBaseDelay = TimeSpan.Zero };

        fetcher.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromException<PageFetchResult>(new TiktokWafException()));

        await using var client = new TiktokClient(fetcher, options);

        try { await client.GetVideoAsync(ValidUrl); } catch (TiktokWafException) { }

        await fetcher.Received(expectedCalls).FetchPageAsync(ValidUrl, Arg.Any<CancellationToken>());
    }
}
