namespace TiktokExplode.IntegrationTests.Videos;

public sealed class TiktokClientTests
{
    [Fact]
    public async Task GetVideoAsync_Should_Return_Video()
    {
        var options = new TiktokOptions
        {
            MaxWafRetries = 10,
            RetryBaseDelay = TimeSpan.FromMilliseconds(200),
        };

        await using var client = TiktokClient.CreateWithHttp(options: options);

        var video = await client.GetVideoAsync(
            "https://www.tiktok.com/@js_nightwave/video/7579504710961548565");

        video.Should().NotBeNull();

        video.Id.Should().NotBeNullOrWhiteSpace();

        video.Info.DownloadLinks.Should().NotBeNull();

        video.Author.Should().NotBeNull();
    }
}
