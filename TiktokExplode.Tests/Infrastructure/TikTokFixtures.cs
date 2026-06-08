using TiktokExplode.Infrastructure.Fetchers;

namespace TiktokExplode.Tests.Infrastructure;

/// <summary>
/// Shared HTML fixtures used across Infrastructure test classes.
/// All JSON values are synthetic — no real user data.
/// </summary>
internal static class TikTokFixtures
{
  // ── Helpers ───────────────────────────────────────────────────────────────

  private static string Wrap(string json) =>
      $"""<!DOCTYPE html><html><body><script id="__UNIVERSAL_DATA_FOR_REHYDRATION__" type="application/json">{json}</script></body></html>""";

  // ── Valid fixture ─────────────────────────────────────────────────────────

  public static readonly string ValidVideoHtml = Wrap("""
        {
          "__DEFAULT_SCOPE__": {
            "webapp.video-detail": {
              "itemInfo": {
                "itemStruct": {
                  "id": "7579504710961548565",
                  "desc": "Nightwave Plaza session",
                  "createTime": "1700000000",
                  "textLanguage": "en",
                  "textTranslatable": true,
                  "locationCreated": "US",
                  "author": {
                    "id": "111111111",
                    "uniqueId": "js_nightwave",
                    "nickname": "Nightwave Plaza",
                    "signature": "Chill music",
                    "verified": false,
                    "privateAccount": false,
                    "createTime": 1600000000,
                    "avatarLarger": "https://example.com/avatar_large.jpg",
                    "avatarMedium": "https://example.com/avatar_medium.jpg",
                    "avatarThumb":  "https://example.com/avatar_thumb.jpg"
                  },
                  "authorStatsV2": {
                    "followerCount": "1000",
                    "followingCount": "500",
                    "friendCount": "50",
                    "heartCount": "50000",
                    "videoCount": "100"
                  },
                  "video": {
                    "ratio": "720p",
                    "videoQuality": "normal",
                    "width": 1080,
                    "height": 1920,
                    "duration": 12,
                    "preciseDuration": 12.345,
                    "cover": "https://cdn.example.com/cover_static.jpg",
                    "dynamicCover": "https://cdn.example.com/cover_animated.webp",
                    "playAddr": "https://cdn.example.com/play/video.mp4",
                    "downloadAddr": "https://cdn.example.com/download/video.mp4",
                    "size": "10485760",
                    "bitrateInfo": [
                      {
                        "Bitrate": 1000000,
                        "BitrateFPS": 30,
                        "Format": "mp4",
                        "CodecType": "h264"
                      }
                    ]
                  },
                  "music": {
                    "id": "9000000000000000001",
                    "title": "Midnight Synth Loop",
                    "playUrl": "https://cdn.example.com/music/audio.mp3",
                    "coverThumb": "https://cdn.example.com/music/cover_thumb.jpg",
                    "coverMedium": "https://cdn.example.com/music/cover_medium.jpg",
                    "coverLarge": "https://cdn.example.com/music/cover_large.jpg",
                    "authorName": "Nightwave Audio",
                    "album": "Synthetic Fixtures",
                    "original": true,
                    "private": false,
                    "duration": 15,
                    "isCopyrighted": false
                  },
                  "statsV2": {
                    "playCount": "100000",
                    "diggCount": "5000",
                    "commentCount": "200",
                    "shareCount": "1000",
                    "collectCount": "300",
                    "repostCount": "50"
                  }
                }
              }
            }
          }
        }
        """);

  public static PageFetchResult ValidVideoResult => new()
  {
    HtmlContent = ValidVideoHtml,
    Cookies = []
  };

  // ── Error fixtures ────────────────────────────────────────────────────────

  /// <summary>Plain HTML without the hydration script tag.</summary>
  public static readonly string NoHydrationScriptHtml =
      "<html><body><p>No script here</p></body></html>";

  /// <summary>Valid JSON structure but <c>itemStruct</c> is absent → <see cref="Domain.Exceptions.VideoNotFoundException"/>.</summary>
  public static readonly string MissingItemStructHtml = Wrap("""
        {
          "__DEFAULT_SCOPE__": {
            "webapp.video-detail": {
              "itemInfo": {}
            }
          }
        }
        """);

  /// <summary>JSON root without <c>__DEFAULT_SCOPE__</c> → <see cref="Domain.Exceptions.TiktokParsingException"/>.</summary>
  public static readonly string MissingDefaultScopeHtml = Wrap("""{"other":"data"}""");
}
