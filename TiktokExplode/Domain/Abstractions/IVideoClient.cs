using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.ValueObjects;

namespace TiktokExplode.Domain.Abstractions;

public interface IVideoClient : IAsyncDisposable
{
    Task<Video> GetVideoAsync(string url, CancellationToken cancellationToken = default);
    Task<StreamInfo> DownloadAsync(Video video, CancellationToken cancellationToken = default);
    Task<StreamInfo> DownloadWatermarkedAsync(Video video, CancellationToken cancellationToken = default);
}
