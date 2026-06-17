namespace TiktokExplode.IntegrationTests.Search;

[Collection("Playwright")]
public class PlaywrightSearchFetcherTests()
{
    [Fact]
    public async Task FetchSearchAsync_Should_Return_First_Page()
    {
        var fetcher = new PlaywrightSearchFetcher();

        await foreach (var page in fetcher.FetchSearchAsync("lalala"))
        {
            page.JsonContent.Should().NotBeNullOrWhiteSpace();

            var root = JsonNode.Parse(page.JsonContent);

            root.Should().NotBeNull();

            break;
        }
    }

    [Fact]
    public async Task FetchSearchAsync_Should_Load_Next_Page_When_HasMore_Is_True()
    {
        var fetcher = new PlaywrightSearchFetcher();

        var pages = new List<SearchFetchResult>();

        await foreach (var page in fetcher.FetchSearchAsync("lalala"))
        {
            pages.Add(page);

            if (pages.Count >= 2)
                break;
        }

        var firstRoot = JsonNode.Parse(pages[0].JsonContent);

        var hasMore = firstRoot?["has_more"]?.GetValue<int>() ?? 0;

        if (hasMore == 1)
        {
            pages.Should().HaveCountGreaterThan(1);
        }
    }

    [Fact]
    public async Task FetchSearchAsync_Should_Return_Cookies()
    {
        var fetcher = new PlaywrightSearchFetcher();

        await foreach (var page in fetcher.FetchSearchAsync("lalala"))
        {
            page.Cookies.Should().NotBeEmpty();
            page.Cookies.Should().Contain(
                c => c.Name.Equals("msToken", StringComparison.OrdinalIgnoreCase));

            break;
        }
    }
}
