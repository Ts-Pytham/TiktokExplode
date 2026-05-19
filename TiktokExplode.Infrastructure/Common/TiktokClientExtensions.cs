using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;

namespace TiktokExplode.Infrastructure.Common;

/// <summary>
/// Extension methods on <see cref="IVideoClient"/> for file-path-based downloads
/// with optional progress reporting.
/// </summary>
public static class TiktokClientExtensions
{
    extension(IVideoClient client)
    {
        /// <summary>
        /// Downloads the video without watermark directly to a file at <paramref name="filePath"/>.
        /// Reports download progress as a 0.0–1.0 fraction via <paramref name="progress"/> if provided.
        /// The exact byte count used for progress is taken from the CDN <c>Content-Length</c> header.
        /// </summary>
        /// <param name="video">The video to download.</param>
        /// <param name="filePath">Path to the output file. The file is created or overwritten.</param>
        /// <param name="progress">Optional callback for progress updates (0.0 = start, 1.0 = complete).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
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

        /// <summary>
        /// Downloads the video with TikTok's watermark directly to a file at <paramref name="filePath"/>.
        /// Reports download progress as a 0.0–1.0 fraction via <paramref name="progress"/> if provided.
        /// </summary>
        /// <param name="video">The video to download.</param>
        /// <param name="filePath">Path to the output file. The file is created or overwritten.</param>
        /// <param name="progress">Optional callback for progress updates (0.0 = start, 1.0 = complete).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
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
