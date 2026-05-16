using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using TiktokExplode.Domain.Exceptions;

namespace TiktokExplode.Infrastructure.Common;

internal static class JsonNodeExtensions
{
    public static string GetString(this JsonNode node, string field)
    {
        var value = node[field]
            ?? throw new TiktokParsingException($"Field '{field}' not found in the JSON content.");

        if (value.GetValueKind() != JsonValueKind.String)
            throw new TiktokParsingException($"Field '{field}' has an unexpected type.");

        return value.GetValue<string>();
    }

    public static string GetStringOrEmpty(this JsonNode node, string field)
    {
        var value = node[field];

        if (value is null || value.GetValueKind() != JsonValueKind.String)
            return string.Empty;

        return value.GetValue<string>();
    }

    public static T GetNumber<T>(this JsonNode node, string field) where T : INumber<T>
    {
        var value = node[field]
            ?? throw new TiktokParsingException($"Field '{field}' not found in the JSON content.");

        return value.GetValueKind() switch
        {
            JsonValueKind.Number => value.GetValue<T>(),
            JsonValueKind.String => T.TryParse(value.GetValue<string>(), null, out var result)
                ? result
                : throw new TiktokParsingException($"Field '{field}' has an invalid numeric value."),
            _ => throw new TiktokParsingException($"Field '{field}' has an unexpected type.")
        };
    }

    public static bool GetBool(this JsonNode node, string field)
    {
        var value = node[field]
            ?? throw new TiktokParsingException($"Field '{field}' not found in the JSON content.");

        return value.GetValueKind() switch
        {
            JsonValueKind.True  => true,
            JsonValueKind.False => false,
            _ => throw new TiktokParsingException($"Field '{field}' has an unexpected type.")
        };
    }
}
