using Microsoft.Playwright;

namespace TiktokExplode.Infrastructure.Fetchers.Search;

/// <summary>
/// Represents the result of fetching a search page, containing the raw JSON content of the page
/// and the corresponding Playwright <see cref="IPage"/> instance for any necessary cookie extraction or further interactions.
/// </summary>
internal sealed class SearchPageResult
{
    /// <summary>
    /// The raw JSON content extracted from the search page, which contains the data needed to parse 
    /// and construct <see cref="Domain.Entities.Video"/> objects.
    /// </summary>
    public required string JsonContent { get; init; }

    /// <summary>
    /// The Playwright <see cref="IPage"/> instance associated with the search page, which can be 
    /// used for any necessary cookie extraction or further interactions.
    /// </summary>
    public required IPage Page { get; init; }
}
