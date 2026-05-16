namespace TiktokExplode.Domain.ValueObjects.Videos;

public sealed record VideoInfo
{
    public string Ratio { get; init; } = string.Empty;
    public Bitrate[] Bitrates { get; init; } = [];
    public string VideoQuality { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public VideoDownloadLinks DownloadLinks { get; init; } = new();
}
