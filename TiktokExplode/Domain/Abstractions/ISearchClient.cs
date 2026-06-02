using TiktokExplode.Domain.Entities;

namespace TiktokExplode.Domain.Abstractions;

/// <summary>
/// Defines the contract for a TikTok search client that queries videos by keyword
/// and can stream their content from TikTok's CDN.
/// </summary>
public interface ISearchClient : IDownloadClient
{
    /// <summary>
    /// Asynchronously streams <see cref="Video"/> results for the given <paramref name="keyword"/>,
    /// lazily fetching additional pages as the sequence is consumed.
    /// </summary>
    /// <param name="keyword">The search term to query on TikTok.</param>
    /// <param name="cancellationToken">Token to cancel the enumeration.</param>
    /// <returns>
    /// An <see cref="IAsyncEnumerable{T}"/> of <see cref="Video"/> values
    /// ordered by TikTok's default relevance ranking.
    /// </returns>
    IAsyncEnumerable<Video> SearchAsync(string keyword, CancellationToken cancellationToken = default);
}