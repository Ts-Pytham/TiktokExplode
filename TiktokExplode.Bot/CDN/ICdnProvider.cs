namespace TiktokExplode.Bot.CDN;

public interface ICdnProvider
{
    string Name { get; }
    Task<string> UploadAsync(byte[] data, string filename, CancellationToken ct = default);
}
