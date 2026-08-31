using System.Text.Json.Nodes;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Enums;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Domain.ValueObjects.Authors;
using TiktokExplode.Domain.ValueObjects.Media;
using TiktokExplode.Domain.ValueObjects.Videos;
using TiktokExplode.Infrastructure.Common;

namespace TiktokExplode.Infrastructure.Parsers;

/// <summary>
/// Parses a TikTok video page HTML string into a fully populated <see cref="Video"/> domain object.
/// Extracts the JSON hydration data embedded in the <c>#__UNIVERSAL_DATA_FOR_REHYDRATION__</c>
/// script tag and maps each field to its corresponding value object.
/// </summary>
internal sealed class TiktokVideoParser
{
    /// <summary>AngleSharp HTML parser used to query the hydration script element.</summary>
    private readonly HtmlParser _htmlParser = new();

    /// <summary>
    /// Parses a TikTok video page HTML string and returns the corresponding <see cref="Video"/>.
    /// </summary>
    /// <param name="htmlContent">The full HTML source of a TikTok video page.</param>
    /// <returns>A fully populated <see cref="Video"/> domain object.</returns>
    /// <exception cref="TiktokParsingException">
    /// Thrown if the hydration script, required JSON nodes, or individual fields are missing or malformed.
    /// </exception>
    /// <exception cref="Domain.Exceptions.VideoNotFoundException">
    /// Thrown when TikTok reports a non-zero <c>statusCode</c>, meaning the video was removed,
    /// is private, or is unavailable in the current region.
    /// </exception>
    /// <exception cref="TiktokUnavailablePageException">
    /// Thrown when TikTok reports success but omits <c>itemStruct</c>, which indicates a soft block.
    /// </exception>
    public async Task<Video> ParseAsync(string htmlContent)
    {
        var document = await _htmlParser.ParseDocumentAsync(htmlContent);

        var script = document.QuerySelector("#__UNIVERSAL_DATA_FOR_REHYDRATION__")
            ?? throw new TiktokParsingException("Hydration script not found in the HTML content.")
            {
                Path = "#__UNIVERSAL_DATA_FOR_REHYDRATION__"
            };

        var node = ExtractItemStruct(script);

        return ParseVideo(node);
    }

    /// <summary>Extracts and navigates the JSON hydration tree to reach the <c>itemStruct</c> node.</summary>
    private static JsonNode ExtractItemStruct(IElement html)
    {
        var root = JsonNode.Parse(html.TextContent)
            ?? throw new TiktokParsingException("Failed to parse JSON content from the hydration script.");

        var defaultScope = root["__DEFAULT_SCOPE__"]
            ?? throw new TiktokParsingException("Default scope not found in the JSON content.")
            {
                Path = "__DEFAULT_SCOPE__"
            };

        var webappVideoDetail = defaultScope["webapp.video-detail"]
            ?? throw new TiktokParsingException("Webapp video detail not found in the JSON content.")
            {
                Path = "__DEFAULT_SCOPE__.webapp.video-detail"
            };

        // TikTok reports why an item is unavailable here; 0 means the item exists.
        var tiktokStatusCode = webappVideoDetail.GetOptionalNumber<int>("statusCode") ?? 0;

        if (tiktokStatusCode != 0)
            throw new VideoNotFoundException(
                $"TikTok reported status code {tiktokStatusCode} for this video. It was removed, is private, or is unavailable in this region.")
            {
                TiktokStatusCode = tiktokStatusCode
            };

        var itemInfo = webappVideoDetail["itemInfo"]
            ?? throw new TiktokUnavailablePageException(
                "TikTok reported success but returned no 'itemInfo'. The request was most likely soft-blocked.");

        var itemStruct = itemInfo["itemStruct"]
            ?? throw new TiktokUnavailablePageException(
                "TikTok reported success but returned no 'itemStruct'. The request was most likely soft-blocked.");

        return itemStruct;
    }

    /// <summary>Maps a top-level <c>itemStruct</c> JSON node to a <see cref="Video"/> domain object.</summary>
    internal static Video ParseVideo(JsonNode node)
    {
        var videoNode = node["video"]
            ?? throw new TiktokParsingException("Video information not found in the JSON content.");

        var musicNode = node["music"]
            ?? throw new TiktokParsingException("Music information not found in the JSON content.");

        var statsNode = node["statsV2"]
            ?? throw new TiktokParsingException("Video stats not found in the JSON content.");

        return new Video
        {
            Id = node.GetString("id"),
            Author = ParseAuthor(node),
            Info = ParseVideoInfo(videoNode),
            Stats = ParseVideoStats(statsNode),
            Duration = ParseVideoDuration(videoNode),
            Music = ParseVideoMusic(musicNode),
            Language = ParseVideoLanguage(node),
            Cover = ParseVideoCover(videoNode),
            Description = node.GetStringOrEmpty("desc"),
            Location = node.GetStringOrEmpty("locationCreated"),
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(node.GetNumber<long>("createTime")),
        };
    }

    /// <summary>Maps the <c>video.duration</c> fields to a <see cref="VideoDuration"/>.</summary>
    private static VideoDuration ParseVideoDuration(JsonNode node)
    {
        var seconds = node.GetNumber<int>("duration");

        return new VideoDuration
        {
            Seconds = seconds,
            PreciseSeconds = node.GetOptionalNumber<double>("preciseDuration") ?? seconds
        };
    }

    /// <summary>Maps the <c>author</c> and <c>authorStatsV2</c> sub-trees to an <see cref="Author"/>.</summary>
    internal static Author ParseAuthor(JsonNode node)
    {
        var author = node["author"]
            ?? throw new TiktokParsingException("Author information not found in the JSON content.");

        var authorStatsNode = node["authorStatsV2"]
            ?? throw new TiktokParsingException("Author stats not found in the JSON content.");

        var createdAtUnix = author.GetOptionalNumber<long>("createTime");
        return new Author
        {
            Id = author.GetString("id"),
            UniqueId = author.GetString("uniqueId"),
            Name = author.GetString("nickname"),
            Description = author.GetStringOrEmpty("signature"),
            IsVerified = author.GetBool("verified"),
            IsPrivate = author.GetBool("privateAccount"),
            CreatedAt = createdAtUnix is not null ? DateTimeOffset.FromUnixTimeSeconds(createdAtUnix.Value) : null,
            Avatar = ParseProfileImageVariants(author),
            Stats = ParseAuthorStats(authorStatsNode),
        };
    }

    /// <summary>Maps the <c>authorStatsV2</c> node to an <see cref="AuthorStats"/>.</summary>
    private static AuthorStats ParseAuthorStats(JsonNode authorStatsNode)
    {
        return new AuthorStats
        {
            Followers = authorStatsNode.GetNumber<long>("followerCount"),
            Following = authorStatsNode.GetNumber<long>("followingCount"),
            Friends = authorStatsNode.GetNumber<long>("friendCount"),
            LikesReceived = authorStatsNode.GetNumber<long>("heartCount"),
            VideoCount = authorStatsNode.GetNumber<long>("videoCount"),
        };
    }

    /// <summary>Extracts the three avatar URL variants (<c>Large</c>, <c>Medium</c>, <c>Thumb</c>) from the author node.</summary>
    private static ProfileImageVariants ParseProfileImageVariants(JsonNode author)
    {
        return new ProfileImageVariants
        {
            Larger = author.GetString("avatarLarger"),
            Medium = author.GetString("avatarMedium"),
            Small = author.GetString("avatarThumb")
        };
    }

    /// <summary>Maps the <c>video</c> sub-tree to a <see cref="VideoInfo"/>.</summary>
    private static VideoInfo ParseVideoInfo(JsonNode node)
    {
        var bitrateInfoNode = node["bitrateInfo"];

        return new VideoInfo
        {
            Ratio = node.GetString("ratio"),
            VideoQuality = node.GetString("videoQuality"),
            Width = node.GetNumber<int>("width"),
            Height = node.GetNumber<int>("height"),
            Bitrates = bitrateInfoNode is null ? [] : ParseBitrates(bitrateInfoNode),
            DownloadLinks = ParseDownloadLinks(node),
        };
    }

    /// <summary>Extracts CDN download URLs and file size from the <c>video</c> node.</summary>
    private static VideoDownloadLinks ParseDownloadLinks(JsonNode node)
    {
        return new VideoDownloadLinks
        {
            OriginalUrl = node.GetStringOrEmpty("playAddr"),
            WatermarkedUrl = node.GetStringOrEmpty("downloadAddr"),
            OriginalSizeInBytes = node.GetNumber<long>("size")
        };
    }

    /// <summary>Maps the <c>statsV2</c> node to a <see cref="MediaStats"/>.</summary>
    internal static MediaStats ParseVideoStats(JsonNode node)
    {
        return new MediaStats
        {
            Views = node.GetNumber<long>("playCount"),
            Likes = node.GetNumber<long>("diggCount"),
            Comments = node.GetNumber<long>("commentCount"),
            Shares = node.GetNumber<long>("shareCount"),
            Favorites = node.GetNumber<long>("collectCount"),
            Reposts = node.GetNumber<long>("repostCount")
        };
    }

    /// <summary>Converts the <c>bitrateInfo</c> JSON array into a <see cref="Bitrate"/> array.</summary>
    private static Bitrate[] ParseBitrates(JsonNode node)
    {
        return [.. node.AsArray()
            .OfType<JsonNode>()
            .Select(bitrateNode => new Bitrate
            {
                Value       = bitrateNode.GetNumber<int>("Bitrate"),
                FPS         = bitrateNode.GetOptionalNumber<int>("BitrateFPS"),
                Format      = ParseBitrateFormat(bitrateNode),
                RawFormat   = bitrateNode.GetOptionalString("Format"),
                CodecType   = bitrateNode.GetString("CodecType")
            })];
    }

    /// <summary>
    /// Parses the <c>Format</c> string from a bitrate node into a <see cref="BitrateFormat"/> enum.
    /// Returns <see cref="BitrateFormat.Unknown"/> for unrecognised format strings.
    /// </summary>
    private static BitrateFormat ParseBitrateFormat(JsonNode bitrateNode)
    {
        var raw = bitrateNode.GetOptionalString("Format");

        return Enum.TryParse<BitrateFormat>(raw, ignoreCase: true, out var format)
            ? format
            : BitrateFormat.Unknown;
    }

    /// <summary>Maps the <c>textLanguage</c> and <c>textTranslatable</c> fields to a <see cref="MediaLanguage"/>.</summary>
    internal static MediaLanguage ParseVideoLanguage(JsonNode node)
    {
        return new MediaLanguage
        {
            PrimaryLanguage = node.GetStringOrEmpty("textLanguage"),
            IsTranslatable = node.GetBool("textTranslatable")
        };
    }

    /// <summary>Maps the <c>cover</c> and <c>dynamicCover</c> fields to a <see cref="MediaCover"/>.</summary>
    internal static MediaCover ParseVideoCover(JsonNode node)
    {
        return new MediaCover
        {
            AnimatedUrl = node.GetStringOrEmpty("dynamicCover"),
            StaticUrl = node.GetString("cover")
        };
    }

    /// <summary>Maps the <c>music</c> sub-tree to a <see cref="MediaMusic"/>.</summary>
    internal static MediaMusic ParseVideoMusic(JsonNode node)
    {
        return new MediaMusic
        {
            Id = node.GetString("id"),
            Title = node.GetString("title"),
            AuthorName = node.GetString("authorName"),
            AlbumName = node.GetStringOrEmpty("album"),
            Images = ParseMusicImageVariants(node),
            Duration = ParseMediaDuration(node),
            IsCopyrighted = node.GetBool("isCopyrighted"),
            IsOriginal = node.GetBool("original"),
            IsPrivate = node.GetBool("private"),
            PlayUrl = node.GetStringOrEmpty("playUrl"),
        };
    }

    /// <summary>Extracts the music duration information, handling both precise and non-precise formats.</summary>
    private static MediaDuration ParseMediaDuration(JsonNode node)
    {
        var seconds = node.GetNumber<int>("duration");
        var preciseDurationNode = node["preciseDuration"];

        return new MediaDuration
        {
            Seconds = seconds,
            PreciseSeconds = preciseDurationNode?.GetOptionalNumber<double>("preciseDuration") ?? seconds
        };
    }

    /// <summary>Extracts the three music cover URL variants (<c>Large</c>, <c>Medium</c>, <c>Thumb</c>) from the music node.</summary>
    private static ProfileImageVariants ParseMusicImageVariants(JsonNode music)
    {
        return new ProfileImageVariants
        {
            Larger = music.GetString("coverLarge"),
            Medium = music.GetString("coverMedium"),
            Small = music.GetString("coverThumb")
        };
    }
}
