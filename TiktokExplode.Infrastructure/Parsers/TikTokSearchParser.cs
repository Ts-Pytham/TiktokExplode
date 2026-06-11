using System.Text.Json.Nodes;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Exceptions;
using TiktokExplode.Domain.ValueObjects.Carousels;
using TiktokExplode.Infrastructure.Common;

namespace TiktokExplode.Infrastructure.Parsers;

/// <summary>
/// Parses TikTok search API responses into domain <see cref="Media"/> objects.
/// Supports both standard video posts and image carousel posts,
/// automatically dispatching each search result to the appropriate parser.
/// </summary>
internal sealed class TiktokSearchParser
{
    /// <summary>
    /// Parses a TikTok search response and returns the media items contained in the result set.
    /// </summary>
    /// <param name="jsonContent">
    /// Raw JSON payload returned by the TikTok search endpoint.
    /// </param>
    /// <returns>
    /// A sequence of <see cref="Media"/> objects representing videos or carousels.
    /// </returns>
    /// <exception cref="TiktokParsingException">
    /// Thrown if the JSON payload cannot be parsed or required nodes are missing.
    /// </exception>
    internal static IEnumerable<Media> Parse(string jsonContent)
    {
        var root = JsonNode.Parse(jsonContent)
            ?? throw new TiktokParsingException("Failed to parse search JSON.");

        var data = root["data"]?.AsArray()
            ?? [];

        foreach (var entry in data)
        {
            if (entry?["type"]?.GetValue<int>() != 1) continue;

            var item = entry["item"]
                ?? throw new TiktokParsingException("'item' node not found in search entry.");

            var imagePost = item["imagePost"];

            if(imagePost is null)
            {
                yield return TiktokVideoParser.ParseVideo(item);
                continue;
            }


            yield return ParseCarousel(item, imagePost);
        }
    }

    /// <summary>
    /// Parses a TikTok carousel post from a search result item.
    /// </summary>
    /// <param name="node">
    /// Root search item node containing metadata, author, stats, and media information.
    /// </param>
    /// <param name="postNode">
    /// The <c>imagePost</c> node containing carousel-specific content.
    /// </param>
    /// <returns>
    /// A fully populated <see cref="Carousel"/> instance.
    /// </returns>
    /// <exception cref="TiktokParsingException">
    /// Thrown if required carousel nodes are missing.
    /// </exception>
    private static Carousel ParseCarousel(JsonNode node, JsonNode postNode)
    {
        var videoNode = node["video"]
            ?? throw new TiktokParsingException("Video information not found in the JSON content.");

        var musicNode = node["music"]
            ?? throw new TiktokParsingException("Music information not found in the JSON content.");

        var statsNode = node["statsV2"]
            ?? throw new TiktokParsingException("Video stats not found in the JSON content.");

        return new Carousel
        {
            Id          = node.GetString("id"),
            Author      = TiktokVideoParser.ParseAuthor(node),
            Stats       = TiktokVideoParser.ParseVideoStats(statsNode),
            Music       = TiktokVideoParser.ParseVideoMusic(musicNode),
            Post        = ParseCarouselPost(postNode),
            Language    = TiktokVideoParser.ParseVideoLanguage(node),
            Cover       = TiktokVideoParser.ParseVideoCover(videoNode),
            Description = node.GetStringOrEmpty("desc"),
            Location    = node.GetStringOrEmpty("locationCreated"),
            CreatedAt   = DateTimeOffset.FromUnixTimeSeconds(node.GetNumber<long>("createTime")),
        };
    }

    /// <summary>
    /// Parses the carousel content section, including cover image and image collection.
    /// </summary>
    /// <param name="node">
    /// The <c>imagePost</c> node from the TikTok payload.
    /// </param>
    /// <returns>
    /// A populated <see cref="CarouselPost"/> instance.
    /// </returns>
    /// <exception cref="TiktokParsingException">
    /// Thrown if the cover image or image collection nodes are missing.
    /// </exception>
    private static CarouselPost ParseCarouselPost(JsonNode node)
    {
        var coverNode = node["cover"]
            ?? throw new TiktokParsingException("Carousel cover information not found in the JSON content.");

        var imageNode = node["images"]
            ?? throw new TiktokParsingException("Carousel images information not found in the JSON content.");

        var images = imageNode.AsArray()
            .OfType<JsonNode>()
            .Select(ParseCarouselImage)
            .ToList();

        return new CarouselPost
        {
            Title       = node.GetString("title"),
            Cover       = ParseCarouselImage(coverNode),
            Images      = images
        };
    }

    /// <summary>
    /// Parses a carousel image definition and extracts its dimensions and URLs.
    /// </summary>
    /// <param name="node">
    /// Image node from the carousel payload.
    /// </param>
    /// <returns>
    /// A populated <see cref="CarouselImage"/> instance.
    /// </returns>
    /// <exception cref="TiktokParsingException">
    /// Thrown if the image URL collection is missing.
    /// </exception>
    private static CarouselImage ParseCarouselImage(JsonNode node)
    {
        var imageUrlNode = node["imageURL"]
            ?? throw new TiktokParsingException("Carousel cover URLs not found in the JSON content.");

        var urls = imageUrlNode["urlList"]?.AsArray()
            .OfType<JsonNode>()
            .Select(urlNode => urlNode.GetValue<string>())
            .ToList();

        return new CarouselImage
        {
            Width   = node.GetNumber<uint>("imageWidth"),
            Height  = node.GetNumber<uint>("imageHeight"),
            Urls    = urls ?? []
        };
    }
}
