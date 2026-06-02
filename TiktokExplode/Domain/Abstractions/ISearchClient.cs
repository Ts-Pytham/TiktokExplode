using TiktokExplode.Domain.Entities;

namespace TiktokExplode.Domain.Abstractions;

public interface ISearchClient : IDownloadClient
{
    IAsyncEnumerable<Video> SearchAsync(string keyword, CancellationToken cancellationToken = default);
}