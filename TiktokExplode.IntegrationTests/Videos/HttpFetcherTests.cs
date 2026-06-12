namespace TiktokExplode.IntegrationTests.Videos;

public sealed class HttpFetcherTests
{
    [Fact]
    public async Task FetchPageAsync_Should_Bypass_Waf_And_Return_Page_Content()
    {
        // Arrange
        using var fetcher = new HttpFetcher();

        // Act
        Func<Task> act = async () =>
        {
            var result = await fetcher.FetchPageAsync(
                "https://www.tiktok.com/@js_nightwave/video/7579504710961548565");

            // Assert
            result.Should().NotBeNull();

            result.HtmlContent.Should().NotBeNullOrWhiteSpace();

            result.HtmlContent.Should()
                .NotContain("_wafchallengeid");

            result.HtmlContent.Length.Should().BeGreaterThan(10_000);

            result.Cookies.Should().NotBeEmpty();
        };

        // Assert
        await act.Should().NotThrowAsync<TiktokWafException>();
    }
}
