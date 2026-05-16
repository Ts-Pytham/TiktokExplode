using TiktokExplode.Domain.ValueObjects.Authors;
using TiktokExplode.Domain.ValueObjects.Videos;

namespace TiktokExplode.Domain.Entities;

public sealed class Video
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Author Author { get; init; } = new();
    public VideoLanguage Language { get; init; } = new();
    public VideoStats Stats { get; init; } = new();
    public VideoInfo Info { get; init; } = new();
    public string Location { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public VideoDuration Duration { get; init; }
}
