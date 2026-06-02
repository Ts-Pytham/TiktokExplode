using System.Text.Json.Nodes;
using TiktokExplode.Domain.Entities;
using TiktokExplode.Domain.Exceptions;

namespace TiktokExplode.Infrastructure.Parsers;

internal sealed class TiktokSearchParser
{
    internal static IEnumerable<Video> Parse(string jsonContent)
    {
        var root = JsonNode.Parse(jsonContent)
            ?? throw new TiktokParsingException("Failed to parse search JSON.");

        var data = root["data"]?.AsArray()
            ?? throw new TiktokParsingException("Search JSON does not contain 'data' array.");

        foreach (var entry in data)
        {
            if (entry?["type"]?.GetValue<int>() != 1) continue;

            var item = entry["item"]
                ?? throw new TiktokParsingException("'item' node not found in search entry.");

            yield return TiktokVideoParser.ParseVideo(item);
        }
    }
}
