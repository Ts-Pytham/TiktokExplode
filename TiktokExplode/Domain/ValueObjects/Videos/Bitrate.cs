using TiktokExplode.Domain.Enums;

namespace TiktokExplode.Domain.ValueObjects.Videos;

public readonly record struct Bitrate
{
    public int Value { get; init; }
    public int FPS { get; init; }
    public BitrateFormat Format { get; init; }
    public string RawFormat { get; init; }
    public string CodecType { get; init; }
}
