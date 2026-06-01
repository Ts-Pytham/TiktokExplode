using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.ValueObjects;

namespace TiktokExplode.Domain.Abstractions;

/// <summary>
/// Defines an interface for downloading TikTok videos, both with and without watermarks.
/// </summary>
public interface IDownloadClient : IAsyncDisposable
{
    /// <summary>
    /// Downloads the video without watermark and returns a <see cref="StreamInfo"/> with the open stream.
    /// The caller is responsible for disposing the returned <see cref="StreamInfo"/>.
    /// </summary>
    /// <param name="video">The <see cref="Video"/> whose download URL will be used.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="StreamInfo"/> containing the open video stream and its exact size in bytes
    /// as reported by the CDN <c>Content-Length</c> header.
    /// </returns>
    Task<StreamInfo> DownloadAsync(Video video, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the video with TikTok's watermark overlay and returns a <see cref="StreamInfo"/> with the open stream.
    /// The caller is responsible for disposing the returned <see cref="StreamInfo"/>.
    /// </summary>
    /// <param name="video">The <see cref="Video"/> whose watermarked download URL will be used.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="StreamInfo"/> containing the open watermarked video stream and its exact size in bytes
    /// as reported by the CDN <c>Content-Length</c> header.
    /// </returns>
    Task<StreamInfo> DownloadWatermarkedAsync(Video video, CancellationToken cancellationToken = default);
}