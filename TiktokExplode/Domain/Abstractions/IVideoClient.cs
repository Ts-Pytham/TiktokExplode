using TiktokExplode.Domain.Entities;

namespace TiktokExplode.Domain.Abstractions;

public interface IVideoClient
{
    Task<Video> GetVideoAsync(string url, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(Video video, CancellationToken cancellationToken = default);
    Task<Stream> DownloadWatermarkedAsync(Video video, CancellationToken cancellationToken = default);
}
