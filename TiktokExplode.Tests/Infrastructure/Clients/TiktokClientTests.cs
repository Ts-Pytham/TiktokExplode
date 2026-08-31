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

    // ── Transient retry ───────────────────────────────────────────────────────

    [Fact]
    public async Task Should_RetryAndSucceed_When_FetcherThrowsWafOnFirstAttemptOnly()
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxRetries = 2, RetryBaseDelay = TimeSpan.Zero };

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
    public async Task Should_RetryAndSucceed_When_FetcherReportsSoftBlockOnFirstAttemptOnly()
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxRetries = 2, RetryBaseDelay = TimeSpan.Zero };

        fetcher.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(
                   Task.FromException<PageFetchResult>(new TiktokUnavailablePageException()),
                   Task.FromResult(TikTokFixtures.ValidVideoResult)
               );

        await using var client = new TiktokClient(fetcher, options);

        var video = await client.GetVideoAsync(ValidUrl);

        video.Should().NotBeNull();
        await fetcher.Received(2).FetchPageAsync(ValidUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_RetryAndSucceed_When_FetcherReturnsRetryableStatusCode()
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxRetries = 2, RetryBaseDelay = TimeSpan.Zero };

        fetcher.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(
                   Task.FromException<PageFetchResult>(TiktokHttpException.FromStatus(503, ValidUrl)),
                   Task.FromResult(TikTokFixtures.ValidVideoResult)
               );

        await using var client = new TiktokClient(fetcher, options);

        var video = await client.GetVideoAsync(ValidUrl);

        video.Should().NotBeNull();
    }

    // ── Terminal failures must not be retried ─────────────────────────────────

    [Fact]
    public async Task Should_NotRetry_When_VideoDoesNotExist()
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxRetries = 5, RetryBaseDelay = TimeSpan.Zero };

        fetcher.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromException<PageFetchResult>(new VideoNotFoundException()));

        await using var client = new TiktokClient(fetcher, options);

        var act = async () => await client.GetVideoAsync(ValidUrl);

        await act.Should().ThrowExactlyAsync<VideoNotFoundException>();
        await fetcher.Received(1).FetchPageAsync(ValidUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotRetry_When_PageStructureChanged()
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxRetries = 5, RetryBaseDelay = TimeSpan.Zero };

        fetcher.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromException<PageFetchResult>(new TiktokParsingException()));

        await using var client = new TiktokClient(fetcher, options);

        var act = async () => await client.GetVideoAsync(ValidUrl);

        await act.Should().ThrowExactlyAsync<TiktokParsingException>();
        await fetcher.Received(1).FetchPageAsync(ValidUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_NotRetry_When_StatusCodeIsTerminal()
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxRetries = 5, RetryBaseDelay = TimeSpan.Zero };

        fetcher.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromException<PageFetchResult>(TiktokHttpException.FromStatus(404, ValidUrl)));

        await using var client = new TiktokClient(fetcher, options);

        var act = async () => await client.GetVideoAsync(ValidUrl);

        (await act.Should().ThrowExactlyAsync<TiktokHttpException>())
            .Which.StatusCode.Should().Be(404);

        await fetcher.Received(1).FetchPageAsync(ValidUrl, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ThrowTiktokWafException_When_AllRetriesAreExhausted()
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxRetries = 2, RetryBaseDelay = TimeSpan.Zero };

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
        int maxRetries,
        int expectedCalls)
    {
        var fetcher = Substitute.For<IPageFetcher>();
        var options = new TiktokOptions { MaxRetries = maxRetries, RetryBaseDelay = TimeSpan.Zero };

        fetcher.FetchPageAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
               .Returns(Task.FromException<PageFetchResult>(new TiktokWafException()));

        await using var client = new TiktokClient(fetcher, options);

        try { await client.GetVideoAsync(ValidUrl); } catch (TiktokWafException) { }

        await fetcher.Received(expectedCalls).FetchPageAsync(ValidUrl, Arg.Any<CancellationToken>());
    }
}
