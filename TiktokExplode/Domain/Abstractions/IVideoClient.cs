using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.ValueObjects;

namespace TiktokExplode.Domain.Abstractions;

/// <summary>
/// Defines the contract for a TikTok video client capable of retrieving metadata
/// and downloading video content.
/// </summary>
public interface IVideoClient : IAsyncDisposable
{
    /// <summary>
    /// Retrieves full metadata for the TikTok video at <paramref name="url"/>.
    /// </summary>
    /// <param name="url">The absolute TikTok video URL (e.g. <c>https://www.tiktok.com/@user/video/123</c>).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A <see cref="Video"/> instance containing all available metadata.</returns>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="url"/> is not a valid TikTok URL.</exception>
    /// <exception cref="Exceptions.TiktokWafException">Thrown when all WAF retry attempts are exhausted.</exception>
    /// <exception cref="Exceptions.VideoNotFoundException">Thrown when the video does not exist or is private.</exception>
    /// <exception cref="Exceptions.TiktokParsingException">Thrown when the page structure cannot be parsed.</exception>
    Task<Video> GetVideoAsync(string url, CancellationToken cancellationToken = default);

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
