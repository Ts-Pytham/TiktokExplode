using Microsoft.Extensions.Logging;

namespace TiktokExplode.Bot.CDN;

/// <summary>
/// Itera los <see cref="ICdnProvider"/> registrados en orden y devuelve la primera URL exitosa.
/// Si todos fallan, lanza <see cref="InvalidOperationException"/>.
/// </summary>
public sealed class CompositeCdnProvider(
    IEnumerable<ICdnProvider> providers,
    ILogger<CompositeCdnProvider> logger)
{
    public async Task<string> UploadAsync(byte[] data, string filename, CancellationToken ct = default)
    {
        foreach (var provider in providers)
        {
            try
            {
                var url = await provider.UploadAsync(data, filename, ct);
                logger.LogInformation("Upload exitoso via {Provider}: {Url}", provider.Name, url);
                return url;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Provider {Provider} falló ({Msg}), probando el siguiente", provider.Name, ex.Message);
            }
        }

        throw new InvalidOperationException("Todos los proveedores CDN fallaron.");
    }
}
