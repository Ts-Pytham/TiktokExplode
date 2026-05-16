namespace TiktokExplode.Domain.ValueObjects.Videos;

public sealed record VideoDownloadLinks
{
    public string OriginalUrl { get; init; } = string.Empty;
    public string WatermarkedUrl { get; init; } = string.Empty;
    public long SizeInBytes { get; init; }
}
