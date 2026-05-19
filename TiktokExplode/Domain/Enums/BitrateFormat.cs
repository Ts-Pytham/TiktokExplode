namespace TiktokExplode.Domain.Enums;

/// <summary>
/// Identifies the container format of a TikTok video encoding variant.
/// </summary>
public enum BitrateFormat
{
    /// <summary>The format string could not be matched to any known value.</summary>
    Unknown = -1,

    /// <summary>MP3 audio container.</summary>
    Mp3 = 0,

    /// <summary>MP4 video container.</summary>
    Mp4 = 1,
}
