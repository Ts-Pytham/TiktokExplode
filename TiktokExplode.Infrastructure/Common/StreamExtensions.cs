using System.Buffers;

namespace TiktokExplode.Infrastructure.Common;

internal static class StreamExtensions
{
    extension(Stream source)
    {
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
