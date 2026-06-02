using System.Text.Json.Nodes;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Enums;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Domain.ValueObjects.Authors;
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
    /// Thrown if the <c>itemStruct</c> node is absent, indicating the video does not exist.
    /// </exception>
    public async Task<Video> ParseAsync(string htmlContent)
    {
        var document = await _htmlParser.ParseDocumentAsync(htmlContent);

        var script = document.QuerySelector("#__UNIVERSAL_DATA_FOR_REHYDRATION__")
            ?? throw new TiktokParsingException("Hydration script not found in the HTML content.");

        var node = ExtractItemStruct(script);

        return ParseVideo(node);
    }

    /// <summary>Extracts and navigates the JSON hydration tree to reach the <c>itemStruct</c> node.</summary>
    private static JsonNode ExtractItemStruct(IElement html)
    {
        var root = JsonNode.Parse(html.TextContent)
            ?? throw new TiktokParsingException("Failed to parse JSON content from the hydration script.");

        var defaultScope = root["__DEFAULT_SCOPE__"]
            ?? throw new TiktokParsingException("Default scope not found in the JSON content.");

        var webappVideoDetail = defaultScope["webapp.video-detail"]
            ?? throw new TiktokParsingException("Webapp video detail not found in the JSON content.");

        var itemInfo = webappVideoDetail["itemInfo"]
            ?? throw new TiktokParsingException("Item info not found in the JSON content.");

        var itemStruct = itemInfo["itemStruct"]
            ?? throw new VideoNotFoundException("Item struct not found in the JSON content.");

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
            Id          = node.GetString("id"),
            Author      = ParseAuthor(node),
            Info        = ParseVideoInfo(videoNode),
            Stats       = ParseVideoStats(statsNode),
            Duration    = ParseVideoDuration(musicNode),
            Language    = ParseVideoLanguage(node),
            Cover       = ParseVideoCover(videoNode),
            Description = node.GetStringOrEmpty("desc"),
            Location    = node.GetStringOrEmpty("locationCreated"),
            CreatedAt   = DateTimeOffset.FromUnixTimeSeconds(node.GetNumber<long>("createTime")),
        };
    }

    /// <summary>Maps the <c>music.preciseDuration</c> sub-tree to a <see cref="VideoDuration"/>.</summary>
    private static VideoDuration ParseVideoDuration(JsonNode node)
    {
        var videoDurationNode = node["preciseDuration"]
            ?? throw new TiktokParsingException("Precise duration information not found in the JSON content.");

        return new VideoDuration
        {
            Seconds = node.GetNumber<int>("duration"),
            PreciseSeconds = videoDurationNode.GetNumber<double>("preciseDuration")
        };
    }

    /// <summary>Maps the <c>author</c> and <c>authorStatsV2</c> sub-trees to an <see cref="Author"/>.</summary>
    private static Author ParseAuthor(JsonNode node)
    {
        var author = node["author"]
            ?? throw new TiktokParsingException("Author information not found in the JSON content.");

        var authorStatsNode = node["authorStatsV2"]
            ?? throw new TiktokParsingException("Author stats not found in the JSON content.");

        return new Author
        {
            Id = author.GetString("id"),
            UniqueId = author.GetString("uniqueId"),
            Name = author.GetString("nickname"),
            Description = author.GetStringOrEmpty("signature"),
            IsVerified = author.GetBool("verified"),
            IsPrivate = author.GetBool("privateAccount"),
            CreatedAt = DateTimeOffset.FromUnixTimeSeconds(author.GetNumber<long>("createTime")),
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
        var bitrateInfoNode = node["bitrateInfo"]
            ?? throw new TiktokParsingException("Video bitrate information not found in the JSON content.");

        return new VideoInfo
        {
            Ratio = node.GetString("ratio"),
            VideoQuality = node.GetString("videoQuality"),
            Width = node.GetNumber<int>("width"),
            Height = node.GetNumber<int>("height"),
            Bitrates = ParseBitrates(bitrateInfoNode),
            DownloadLinks = ParseDownloadLinks(node),
        };
    }

    /// <summary>Extracts CDN download URLs and file size from the <c>video</c> node.</summary>
    private static VideoDownloadLinks ParseDownloadLinks(JsonNode node)
    {
        return new VideoDownloadLinks
        {
            OriginalUrl = node.GetString("playAddr"),
            WatermarkedUrl = node.GetString("downloadAddr"),
            OriginalSizeInBytes = node.GetNumber<long>("size")
        };
    }

    /// <summary>Maps the <c>statsV2</c> node to a <see cref="VideoStats"/>.</summary>
    private static VideoStats ParseVideoStats(JsonNode node)
    {
        return new VideoStats
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
                Value = bitrateNode.GetNumber<int>("Bitrate"),
                FPS = bitrateNode.GetNumber<int>("BitrateFPS"),
                Format = ParseBitrateFormat(bitrateNode),
                RawFormat = bitrateNode.GetString("Format"),
                CodecType = bitrateNode.GetString("CodecType")
            })];
    }

    /// <summary>
    /// Parses the <c>Format</c> string from a bitrate node into a <see cref="BitrateFormat"/> enum.
    /// Returns <see cref="BitrateFormat.Unknown"/> for unrecognised format strings.
    /// </summary>
    private static BitrateFormat ParseBitrateFormat(JsonNode bitrateNode)
    {
        var raw = bitrateNode.GetString("Format");
        return Enum.TryParse<BitrateFormat>(raw, ignoreCase: true, out var format)
            ? format
            : BitrateFormat.Unknown;
    }

    /// <summary>Maps the <c>textLanguage</c> and <c>textTranslatable</c> fields to a <see cref="VideoLanguage"/>.</summary>
    private static VideoLanguage ParseVideoLanguage(JsonNode node)
    {
        return new VideoLanguage
        {
            PrimaryLanguage = node.GetStringOrEmpty("textLanguage"),
            IsTranslatable = node.GetBool("textTranslatable")
        };
    }

    private static VideoCover ParseVideoCover(JsonNode node)
    {
        return new VideoCover
        {
            AnimatedUrl = node.GetString("dynamicCover"),
            StaticUrl = node.GetString("cover")
        };
    }
}