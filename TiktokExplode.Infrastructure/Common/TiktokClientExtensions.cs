using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;

namespace TiktokExplode.Infrastructure.Common;

public static class TiktokClientExtensions
{
    extension(IVideoClient client)
    {
        public async Task DownloadAsync(
            Video video, 
            string filePath, 
            IProgress<double>? progress = null, 
            CancellationToken cancellationToken = default)
        {
            await using var destination = File.Create(filePath);
            await using var streamInfo = await client.DownloadAsync(
                video, cancellationToken);
            await streamInfo.Stream.CopyToAsync(destination, streamInfo.ContentLength, progress, cancellationToken);
        }

        public async Task DownloadWatermarkedAsync(
            Video video,
            string filePath,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            await using var destination = File.Create(filePath);
            await using var streamInfo = await client.DownloadWatermarkedAsync(
                video, cancellationToken);
            await streamInfo.Stream.CopyToAsync(destination, streamInfo.ContentLength, progress, cancellationToken);
        }
    }
}
