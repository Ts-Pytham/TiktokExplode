namespace TiktokExplode.IntegrationTests.Search;

[Collection("Playwright")]
public class TiktokSearchClientTests
{
    [Fact]
    public async Task SearchAsync_Should_Return_Media()
    {
        var client = new TiktokSearchClient();

        var media = new List<Media>();

        await foreach (var item in client.SearchAsync("lalala"))
        {
            media.Add(item);

            if (media.Count >= 5)
                break;
        }

        media.Should().NotBeEmpty();
        media.Should().OnlyContain(m =>
            !string.IsNullOrWhiteSpace(m.Id));
    }
}
