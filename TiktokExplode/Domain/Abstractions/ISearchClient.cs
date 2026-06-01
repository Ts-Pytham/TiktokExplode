using TiktokExplode.Domain.Entities;

namespace TiktokExplode.Domain.Abstractions;

public interface ISearchClient : IDownloadClient
{
    IAsyncEnumerable<Video> SearchVideosAsync(string query, CancellationToken cancellationToken = default);
}