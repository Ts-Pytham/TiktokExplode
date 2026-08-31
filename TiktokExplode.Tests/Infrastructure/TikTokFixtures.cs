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

  /// <summary>
  /// TikTok reports success but omits <c>itemStruct</c> → soft block →
  /// <see cref="Domain.Exceptions.TiktokUnavailablePageException"/>.
  /// </summary>
  public static readonly string MissingItemStructHtml = Wrap("""
        {
          "__DEFAULT_SCOPE__": {
            "webapp.video-detail": {
              "statusCode": 0,
              "itemInfo": {}
            }
          }
        }
        """);

  /// <summary>
  /// TikTok reports a non-zero <c>statusCode</c> → the item is gone →
  /// <see cref="Domain.Exceptions.VideoNotFoundException"/>.
  /// </summary>
  public static readonly string ItemUnavailableHtml = Wrap("""
        {
          "__DEFAULT_SCOPE__": {
            "webapp.video-detail": {
              "statusCode": 10204,
              "itemInfo": {}
            }
          }
        }
        """);

  /// <summary>JSON root without <c>__DEFAULT_SCOPE__</c> → <see cref="Domain.Exceptions.TiktokParsingException"/>.</summary>
  public static readonly string MissingDefaultScopeHtml = Wrap("""{"other":"data"}""");

  // ── Search fixtures ───────────────────────────────────────────────────────

  /// <summary>A synthetic author block reused across search fixtures.</summary>
  private const string SearchAuthorBlock = """
        "author": {
          "id": "888888888",
          "uniqueId": "search_user",
          "nickname": "Search User",
          "signature": "Search bio",
          "verified": true,
          "privateAccount": false,
          "createTime": 1600000000,
          "avatarLarger": "https://example.com/sv_avatar_large.jpg",
          "avatarMedium": "https://example.com/sv_avatar_medium.jpg",
          "avatarThumb":  "https://example.com/sv_avatar_thumb.jpg"
        },
        "authorStatsV2": {
          "followerCount": "2000",
          "followingCount": "300",
          "friendCount": "25",
          "heartCount": "75000",
          "videoCount": "50"
        }
        """;

  /// <summary>A synthetic music block reused across search fixtures.</summary>
  private const string SearchMusicBlock = """
        "music": {
          "id": "7000000000000000001",
          "title": "Search Track",
          "playUrl": "https://cdn.example.com/sv_music.mp3",
          "coverThumb":  "https://cdn.example.com/sv_music_thumb.jpg",
          "coverMedium": "https://cdn.example.com/sv_music_medium.jpg",
          "coverLarge":  "https://cdn.example.com/sv_music_large.jpg",
          "authorName": "Search Artist",
          "album": "Search Album",
          "original": false,
          "private": false,
          "duration": 20,
          "isCopyrighted": true
        }
        """;

  /// <summary>A synthetic stats block reused across search fixtures.</summary>
  private const string SearchStatsBlock = """
        "statsV2": {
          "playCount":   "200000",
          "diggCount":   "8000",
          "commentCount":"500",
          "shareCount":  "2000",
          "collectCount":"600",
          "repostCount": "100"
        }
        """;

  /// <summary>Search JSON payload containing a single video result.</summary>
  public static readonly string ValidVideoSearchJson = $$"""
        {
          "data": [
            {
              "type": 1,
              "item": {
                "id": "1111111111111111111",
                "desc": "Search video description",
                "createTime": 1700100000,
                "textLanguage": "es",
                "textTranslatable": false,
                "locationCreated": "MX",
                {{SearchAuthorBlock}},
                "video": {
                  "ratio": "1080p",
                  "videoQuality": "normal",
                  "width": 1080,
                  "height": 1920,
                  "duration": 20,
                  "preciseDuration": 20.75,
                  "cover": "https://cdn.example.com/sv_cover.jpg",
                  "dynamicCover": "https://cdn.example.com/sv_cover.webp",
                  "playAddr":    "https://cdn.example.com/sv_play.mp4",
                  "downloadAddr":"https://cdn.example.com/sv_download.mp4",
                  "size": "5242880",
                  "bitrateInfo": [
                    {
                      "Bitrate": 2000000,
                      "BitrateFPS": 60,
                      "Format": "mp4",
                      "CodecType": "h265"
                    }
                  ]
                },
                {{SearchMusicBlock}},
                {{SearchStatsBlock}}
              }
            }
          ]
        }
        """;

  /// <summary>Search JSON payload containing a single carousel (slideshow) result.</summary>
  public static readonly string ValidCarouselSearchJson = $$"""
        {
          "data": [
            {
              "type": 1,
              "item": {
                "id": "2222222222222222222",
                "desc": "Search carousel description",
                "createTime": 1700200000,
                "textLanguage": "en",
                "textTranslatable": true,
                "locationCreated": "US",
                "author": {
                  "id": "777777777",
                  "uniqueId": "carousel_user",
                  "nickname": "Carousel User",
                  "signature": "Carousel bio",
                  "verified": false,
                  "privateAccount": false,
                  "createTime": 1610000000,
                  "avatarLarger": "https://example.com/cu_avatar_large.jpg",
                  "avatarMedium": "https://example.com/cu_avatar_medium.jpg",
                  "avatarThumb":  "https://example.com/cu_avatar_thumb.jpg"
                },
                "authorStatsV2": {
                  "followerCount": "3000",
                  "followingCount": "150",
                  "friendCount": "10",
                  "heartCount": "20000",
                  "videoCount": "30"
                },
                "video": {
                  "cover": "https://cdn.example.com/cu_video_cover.jpg",
                  "dynamicCover": "https://cdn.example.com/cu_video_animated.webp"
                },
                "music": {
                  "id": "6000000000000000001",
                  "title": "Carousel Track",
                  "playUrl": "https://cdn.example.com/cu_music.mp3",
                  "coverThumb":  "https://cdn.example.com/cu_music_thumb.jpg",
                  "coverMedium": "https://cdn.example.com/cu_music_medium.jpg",
                  "coverLarge":  "https://cdn.example.com/cu_music_large.jpg",
                  "authorName": "Carousel Artist",
                  "album": "Carousel Album",
                  "original": true,
                  "private": false,
                  "duration": 30,
                  "isCopyrighted": false
                },
                "statsV2": {
                  "playCount":   "50000",
                  "diggCount":   "2000",
                  "commentCount":"100",
                  "shareCount":  "500",
                  "collectCount":"150",
                  "repostCount": "20"
                },
                "imagePost": {
                  "title": "Carousel Title",
                  "cover": {
                    "imageWidth": 1080,
                    "imageHeight": 1920,
                    "imageURL": {
                      "urlList": ["https://cdn.example.com/carousel_cover.jpg"]
                    }
                  },
                  "images": [
                    {
                      "imageWidth": 1080,
                      "imageHeight": 1920,
                      "imageURL": {
                        "urlList": ["https://cdn.example.com/slide1.jpg"]
                      }
                    },
                    {
                      "imageWidth": 720,
                      "imageHeight": 1280,
                      "imageURL": {
                        "urlList": ["https://cdn.example.com/slide2.jpg", "https://cdn.example.com/slide2_fallback.jpg"]
                      }
                    }
                  ]
                }
              }
            }
          ]
        }
        """;

  /// <summary>
  /// Search JSON with a type=1 video entry plus a type=2 entry that must be skipped.
  /// Expected parsed count: 1 (only the type=1 item).
  /// </summary>
  public static readonly string SearchJsonWithNonTypeOneEntry = $$"""
        {
          "data": [
            {
              "type": 2,
              "item": { "id": "should_be_skipped" }
            },
            {
              "type": 1,
              "item": {
                "id": "1111111111111111111",
                "desc": "Search video description",
                "createTime": 1700100000,
                "textLanguage": "es",
                "textTranslatable": false,
                "locationCreated": "MX",
                {{SearchAuthorBlock}},
                "video": {
                  "ratio": "1080p",
                  "videoQuality": "normal",
                  "width": 1080,
                  "height": 1920,
                  "duration": 20,
                  "preciseDuration": 20.75,
                  "cover": "https://cdn.example.com/sv_cover.jpg",
                  "dynamicCover": "https://cdn.example.com/sv_cover.webp",
                  "playAddr":    "https://cdn.example.com/sv_play.mp4",
                  "downloadAddr":"https://cdn.example.com/sv_download.mp4",
                  "size": "5242880",
                  "bitrateInfo": [
                    { "Bitrate": 2000000, "BitrateFPS": 60, "Format": "mp4", "CodecType": "h265" }
                  ]
                },
                {{SearchMusicBlock}},
                {{SearchStatsBlock}}
              }
            }
          ]
        }
        """;

  /// <summary>Search JSON with both a video and a carousel entry (both type=1).</summary>
  public static readonly string MixedVideoAndCarouselSearchJson = $$"""
        {
          "data": [
            {
              "type": 1,
              "item": {
                "id": "1111111111111111111",
                "desc": "Search video description",
                "createTime": 1700100000,
                "textLanguage": "es",
                "textTranslatable": false,
                "locationCreated": "MX",
                {{SearchAuthorBlock}},
                "video": {
                  "ratio": "1080p",
                  "videoQuality": "normal",
                  "width": 1080,
                  "height": 1920,
                  "duration": 20,
                  "preciseDuration": 20.75,
                  "cover": "https://cdn.example.com/sv_cover.jpg",
                  "dynamicCover": "https://cdn.example.com/sv_cover.webp",
                  "playAddr":    "https://cdn.example.com/sv_play.mp4",
                  "downloadAddr":"https://cdn.example.com/sv_download.mp4",
                  "size": "5242880",
                  "bitrateInfo": [
                    { "Bitrate": 2000000, "BitrateFPS": 60, "Format": "mp4", "CodecType": "h265" }
                  ]
                },
                {{SearchMusicBlock}},
                {{SearchStatsBlock}}
              }
            },
            {
              "type": 1,
              "item": {
                "id": "2222222222222222222",
                "desc": "Search carousel description",
                "createTime": 1700200000,
                "textLanguage": "en",
                "textTranslatable": true,
                "locationCreated": "US",
                "author": {
                  "id": "777777777",
                  "uniqueId": "carousel_user",
                  "nickname": "Carousel User",
                  "signature": "",
                  "verified": false,
                  "privateAccount": false,
                  "createTime": 1610000000,
                  "avatarLarger": "https://example.com/cu_avatar_large.jpg",
                  "avatarMedium": "https://example.com/cu_avatar_medium.jpg",
                  "avatarThumb":  "https://example.com/cu_avatar_thumb.jpg"
                },
                "authorStatsV2": {
                  "followerCount": "3000",
                  "followingCount": "150",
                  "friendCount": "10",
                  "heartCount": "20000",
                  "videoCount": "30"
                },
                "video": {
                  "cover": "https://cdn.example.com/cu_video_cover.jpg",
                  "dynamicCover": "https://cdn.example.com/cu_video_animated.webp"
                },
                "music": {
                  "id": "6000000000000000001",
                  "title": "Carousel Track",
                  "playUrl": "https://cdn.example.com/cu_music.mp3",
                  "coverThumb":  "https://cdn.example.com/cu_music_thumb.jpg",
                  "coverMedium": "https://cdn.example.com/cu_music_medium.jpg",
                  "coverLarge":  "https://cdn.example.com/cu_music_large.jpg",
                  "authorName": "Carousel Artist",
                  "album": "Carousel Album",
                  "original": true,
                  "private": false,
                  "duration": 30,
                  "isCopyrighted": false
                },
                "statsV2": {
                  "playCount":   "50000",
                  "diggCount":   "2000",
                  "commentCount":"100",
                  "shareCount":  "500",
                  "collectCount":"150",
                  "repostCount": "20"
                },
                "imagePost": {
                  "title": "Carousel Title",
                  "cover": {
                    "imageWidth": 1080,
                    "imageHeight": 1920,
                    "imageURL": { "urlList": ["https://cdn.example.com/carousel_cover.jpg"] }
                  },
                  "images": [
                    {
                      "imageWidth": 1080,
                      "imageHeight": 1920,
                      "imageURL": { "urlList": ["https://cdn.example.com/slide1.jpg"] }
                    }
                  ]
                }
              }
            }
          ]
        }
        """;

  /// <summary>Search JSON with an empty <c>data</c> array → no results.</summary>
  public static readonly string EmptyDataSearchJson = """{ "data": [] }""";

  /// <summary>Search JSON without a <c>data</c> node → no results (parser falls back to empty array).</summary>
  public static readonly string MissingDataSearchJson = """{ "other": "value" }""";

  /// <summary>Search JSON with a type=1 entry that is missing its <c>item</c> node → <see cref="Domain.Exceptions.TiktokParsingException"/>.</summary>
  public static readonly string MissingItemSearchJson = """{ "data": [{ "type": 1 }] }""";
}
