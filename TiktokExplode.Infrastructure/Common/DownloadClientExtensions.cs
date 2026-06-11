using AngleSharp.Dom;
using TiktokExplode.Domain.Abstractions;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Domain.ValueObjects;
using TiktokExplode.Domain.ValueObjects.Carousels;

namespace TiktokExplode.Infrastructure.Common;

/// <summary>
/// Extension methods on <see cref="IDownloadClient"/> for file-path-based downloads
/// with optional progress reporting.
/// </summary>
public static class DownloadClientExtensions
{
    private static readonly HttpClient _httpClient = new();

    extension(IDownloadClient client)
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

        /// <summary>
        /// Downloads the carousel image directly to a file at <paramref name="filePath"/>.
        /// Reports download progress as a 0.0–1.0 fraction via <paramref name="progress"/> if provided.
        /// </summary>
        /// <param name="image">The carousel image to download.</param>
        /// <param name="filePath">Path to the output file. The file is created or overwritten.</param>
        /// <param name="progress">Optional callback for progress updates (0.0 = start, 1.0 = complete).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        public async Task DownloadImageAsync(
            CarouselImage image,
            string filePath = "image.jpg",
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            await using var destination = File.Create(filePath);
            await using var streamInfo = await client.DownloadImageAsync(image, cancellationToken);

            await streamInfo.Stream.CopyToAsync(destination, streamInfo.ContentLength, progress, cancellationToken);
        }

        /// <summary>
        /// Downloads all images of the carousel to files in <paramref name="directoryPath"/>.
        /// Files are named <c>image_1.jpg</c>, <c>image_2.jpg</c>, etc. The directory is created if it doesn't exist.
        /// Reports overall download progress as a 0.0–1.0 fraction via <paramref name="progress"/> if provided.
        /// </summary>
        /// <param name="carousel">The carousel containing the images to download.</param>
        /// <param name="directoryPath">Path to the directory where images will be saved.</param>
        /// <param name="progress">Optional callback for progress updates (0.0 = start, 1.0 = complete).</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        public async Task DownloadCarouselImagesAsync(
            Carousel carousel,
            string directoryPath,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(directoryPath);

            var images = carousel.Post.Images;

            if (images.Count == 0)
            {
                progress?.Report(1.0);
                return;
            }

            for (int i = 0; i < images.Count; i++)
            {
                var image = images[i];
                var filePath = Path.Combine(directoryPath, $"image_{i + 1}.jpg");

                var completedImages = i;

                var imageProgress = progress is null
                    ? null
                    : new Progress<double>(p =>
                    {
                        var overallProgress =
                            (completedImages + p) / images.Count;

                        progress.Report(overallProgress);
                    });

                await using var destination = File.Create(filePath);

                await using var streamInfo =
                    await client.DownloadImageAsync(image, cancellationToken);

                await streamInfo.Stream.CopyToAsync(
                    destination,
                    streamInfo.ContentLength,
                    imageProgress,
                    cancellationToken);
            }

            progress?.Report(1.0);
        }

        /// <summary>
        /// Downloads the static cover image of <paramref name="video"/> to a file at <paramref name="filePath"/>.
        /// </summary>
        /// <param name="video">The video whose cover image to download.</param>
        /// <param name="filePath">Path to the output file. The file is created or overwritten.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        public async Task DownloadImageAsync(
            Video video,
            string filePath = "cover.jpg",
            CancellationToken cancellationToken = default)
        {
            await using var destination = File.Create(filePath);
            var url = video.Cover.StaticUrl;

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new TiktokException($"Failed to download image from {url}. Status code: {response.StatusCode}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await stream.CopyToAsync(destination, cancellationToken);
        }

        /// <summary>
        /// Downloads the animated cover image (animated WebP) of <paramref name="video"/> to a file at <paramref name="filePath"/>.
        /// </summary>
        /// <remarks>TikTok serves animated covers as WebP — open with a browser or an image viewer that supports animated WebP.</remarks>
        /// <param name="video">The video whose animated cover to download.</param>
        /// <param name="filePath">Path to the output file. The file is created or overwritten.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        public async Task DownloadAnimatedImageAsync(
            Video video,
            string filePath = "cover.webp",
            CancellationToken cancellationToken = default)
        {
            await using var destination = File.Create(filePath);
            var url = video.Cover.AnimatedUrl;
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new TiktokException($"Failed to download animated image from {url}. Status code: {response.StatusCode}");
            }
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await stream.CopyToAsync(destination, cancellationToken);
        }
    }
}
