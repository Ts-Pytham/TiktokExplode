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

internal sealed class TikTokVideoParser
{
    private readonly HtmlParser _htmlParser = new();
    public async Task<Video> ParseAsync(string htmlContent) 
    {
        var document = await _htmlParser.ParseDocumentAsync(htmlContent);

        var script = document.QuerySelector("#__UNIVERSAL_DATA_FOR_REHYDRATION__")
            ?? throw new TiktokParsingException("Hydration script not found in the HTML content.");

        var node = ExtractItemStruct(script);
        
        return ParseVideo(node);
    }

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

    private static Video ParseVideo(JsonNode node)
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
            Description = node.GetStringOrEmpty("desc"),
            Location    = node.GetStringOrEmpty("locationCreated"),
            CreatedAt   = DateTimeOffset.FromUnixTimeSeconds(node.GetNumber<long>("createTime")),
        };
    }

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

    private static Author ParseAuthor(JsonNode node)
    {
        var author = node["author"]
            ?? throw new TiktokParsingException("Author information not found in the JSON content.");

        var authorStatsNode = node["authorStatsV2"]
            ?? throw new TiktokParsingException("Author stats not found in the JSON content.");

        return new Author
        {
            Id          = author.GetString("id"),
            UniqueId    = author.GetString("uniqueId"),
            Name        = author.GetString("nickname"),
            Description = author.GetStringOrEmpty("signature"),
            IsVerified  = author.GetBool("verified"),
            IsPrivate   = author.GetBool("privateAccount"),
            CreatedAt   = DateTimeOffset.FromUnixTimeSeconds(author.GetNumber<long>("createTime")),
            Avatar      = ParseProfileImageVariants(author),
            Stats       = ParseAuthorStats(authorStatsNode),
        };
    }

    private static AuthorStats ParseAuthorStats(JsonNode authorStatsNode)
    {
        return new AuthorStats
        {
            Followers     = authorStatsNode.GetNumber<long>("followerCount"),
            Following     = authorStatsNode.GetNumber<long>("followingCount"),
            Friends       = authorStatsNode.GetNumber<long>("friendCount"),
            LikesReceived = authorStatsNode.GetNumber<long>("heartCount"),
            VideoCount    = authorStatsNode.GetNumber<long>("videoCount"),
        };
    }

    private static ProfileImageVariants ParseProfileImageVariants(JsonNode author)
    {
        return new ProfileImageVariants
        {
            Larger = author.GetString("avatarLarger"),
            Medium = author.GetString("avatarMedium"),
            Small  = author.GetString("avatarThumb")
        };
    }

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

    private static VideoDownloadLinks ParseDownloadLinks(JsonNode node)
    {
        return new VideoDownloadLinks
        {
            OriginalUrl     = node.GetString("playAddr"),
            WatermarkedUrl  = node.GetString("downloadAddr"),
            OriginalSizeInBytes     = node.GetNumber<long>("size")
        };
    }

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

    private static BitrateFormat ParseBitrateFormat(JsonNode bitrateNode)
    {
        var raw = bitrateNode.GetString("Format");
        return Enum.TryParse<BitrateFormat>(raw, ignoreCase: true, out var format)
            ? format
            : BitrateFormat.Unknown;
    }

    private static VideoLanguage ParseVideoLanguage(JsonNode node)
    {
        return new VideoLanguage
        {
            PrimaryLanguage = node.GetStringOrEmpty("textLanguage"),
            IsTranslatable = node.GetBool("textTranslatable")
        };
    }
}