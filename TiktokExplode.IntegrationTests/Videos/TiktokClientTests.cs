namespace TiktokExplode.IntegrationTests.Videos;

public sealed class TiktokClientTests
{
    private static TiktokOptions ResilientOptions => new()
    {
        MaxRetries = 10,
        RetryBaseDelay = TimeSpan.FromMilliseconds(200),
    };

    [Fact]
    public async Task GetVideoAsync_Should_Return_Video()
    {
        await using var client = TiktokClient.CreateWithHttp(options: ResilientOptions);

        var video = await client.GetVideoAsync(
            "https://www.tiktok.com/@js_nightwave/video/7579504710961548565");

        video.Should().NotBeNull();

        video.Id.Should().NotBeNullOrWhiteSpace();

        video.Info.DownloadLinks.Should().NotBeNull();

        video.Author.Should().NotBeNull();
    }

    // Short links do not carry the video id: they only work if the fetcher follows the redirect.
    [Theory]
    [InlineData("https://vm.tiktok.com/ZNdA9gwXk/")]
    [InlineData("https://vt.tiktok.com/ZSAY3fq6L/")]
    public async Task GetVideoAsync_Should_Resolve_ShortLinks(string shortUrl)
    {
        await using var client = TiktokClient.CreateWithHttp(options: ResilientOptions);

        var video = await client.GetVideoAsync(shortUrl);

        video.Should().NotBeNull();
        video.Id.Should().NotBeNullOrWhiteSpace();
        video.Author.UniqueId.Should().NotBeNullOrWhiteSpace();
        video.Url.Should().StartWith("https://www.tiktok.com/@");
    }
}
