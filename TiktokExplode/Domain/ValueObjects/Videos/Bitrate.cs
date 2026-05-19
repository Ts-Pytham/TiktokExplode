using TiktokExplode.Domain.Enums;

namespace TiktokExplode.Domain.ValueObjects.Videos;

/// <summary>
/// Describes a single encoding variant available for a TikTok video.
/// Each video typically has multiple bitrate entries at different quality levels.
/// </summary>
public readonly record struct Bitrate
{
    /// <summary>The bitrate of this encoding variant in bits per second.</summary>
    public int Value { get; init; }

    /// <summary>The frames-per-second rate of this encoding variant.</summary>
    public int FPS { get; init; }

    /// <summary>The parsed container format of this encoding variant.</summary>
    public BitrateFormat Format { get; init; }

    /// <summary>
    /// The raw format string as returned by TikTok's API (e.g. <c>"mp4"</c>, <c>"mp3"</c>).
    /// Use this when <see cref="Format"/> returns <see cref="BitrateFormat.Unknown"/>.
    /// </summary>
    public string RawFormat { get; init; }

    /// <summary>The codec type identifier string (e.g. <c>"h264"</c>).</summary>
    public string CodecType { get; init; }
}
