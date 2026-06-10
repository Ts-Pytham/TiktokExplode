using Microsoft.Playwright;

namespace TiktokExplode.Infrastructure.Fetchers.Search;

internal sealed class SearchPageResult
{
    public required string JsonContent { get; init; }

    public required IPage Page { get; init; }
}
