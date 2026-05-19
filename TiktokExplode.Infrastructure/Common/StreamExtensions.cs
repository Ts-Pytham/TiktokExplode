using System.Buffers;

namespace TiktokExplode.Infrastructure.Common;

/// <summary>
/// Internal extension methods for <see cref="Stream"/> that support progress-reporting copying.
/// Uses <see cref="MemoryPool{T}"/> for buffer allocation to reduce GC pressure.
/// </summary>
internal static class StreamExtensions
{
    extension(Stream source)
    {
        /// <summary>
        /// Copies all bytes from <paramref name="source"/> to <paramref name="destination"/>,
        /// reporting progress to <paramref name="progress"/> as a 0.0–1.0 fraction after each write.
        /// Progress is only reported when <paramref name="sourceLength"/> is greater than zero.
        /// </summary>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="sourceLength">
        /// The known total byte count of <paramref name="source"/>.
        /// Pass <c>-1</c> or any value ≤ 0 to skip progress reporting.
        /// </param>
        /// <param name="progress">
        /// Callback invoked after each buffer write with the fraction copied so far.
        /// May be <see langword="null"/>.
        /// </param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        public async ValueTask CopyToAsync(
            Stream destination,
            long sourceLength,
            IProgress<double>? progress,
            CancellationToken cancellationToken = default
        )
        {
            using var buffer = MemoryPool<byte>.Shared.Rent(81920);

            var totalBytesRead = 0L;

            while (true)
            {
                var bytesRead = await source
                    .ReadAsync(buffer.Memory, cancellationToken)
                    .ConfigureAwait(false);

                if (bytesRead <= 0)
                {
                    break;
                }

                await destination
                    .WriteAsync(buffer.Memory[..bytesRead], cancellationToken)
                    .ConfigureAwait(false);

                totalBytesRead += bytesRead;

                if (progress is not null && sourceLength > 0)
                {
                    progress.Report(1.0 * totalBytesRead / sourceLength);
                }
            }
        }

        /// <summary>
        /// Copies all bytes from <paramref name="source"/> to <paramref name="destination"/>,
        /// auto-detecting the source length via <see cref="Stream.Length"/> when the stream is seekable.
        /// Progress is not reported for non-seekable streams; use the explicit-length overload
        /// (e.g. from <c>StreamInfo.ContentLength</c>) to enable progress in that case.
        /// </summary>
        /// <param name="destination">The stream to write to.</param>
        /// <param name="progress">
        /// Callback invoked after each buffer write with the fraction copied so far.
        /// May be <see langword="null"/>.
        /// </param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        public async ValueTask CopyToAsync(
            Stream destination,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default
        )
        {
            var sourceLength = source.CanSeek ? source.Length : -1;
            await source
                .CopyToAsync(destination, sourceLength, progress, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
