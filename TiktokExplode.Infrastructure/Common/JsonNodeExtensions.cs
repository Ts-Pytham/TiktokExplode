using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using TiktokExplode.Domain.Exceptions;

namespace TiktokExplode.Infrastructure.Common;

/// <summary>
/// Internal extension methods for <see cref="System.Text.Json.Nodes.JsonNode"/> that perform
/// type-checked field reads and throw <see cref="TiktokParsingException"/> on missing or
/// mistyped values.
/// </summary>
internal static class JsonNodeExtensions
{
    /// <summary>
    /// Returns the string value of <paramref name="field"/> in <paramref name="node"/>.
    /// </summary>
    /// <exception cref="TiktokParsingException">Thrown if the field is absent or not a JSON string.</exception>
    public static string GetString(this JsonNode node, string field)
    {
        var value = node[field]
            ?? throw new TiktokParsingException($"Field '{field}' not found in the JSON content.");

        if (value.GetValueKind() != JsonValueKind.String)
            throw new TiktokParsingException($"Field '{field}' has an unexpected type.");

        return value.GetValue<string>();
    }

    /// <summary>
    /// Returns the string value of <paramref name="field"/> in <paramref name="node"/>,
    /// or <see cref="string.Empty"/> if the field is absent or not a JSON string.
    /// </summary>
    public static string GetStringOrEmpty(this JsonNode node, string field)
    {
        var value = node[field];

        if (value is null || value.GetValueKind() != JsonValueKind.String)
            return string.Empty;

        return value.GetValue<string>();
    }

    /// <summary>
    /// Returns the numeric value of <paramref name="field"/> in <paramref name="node"/> as
    /// <typeparamref name="T"/>. JSON strings that parse as a valid number are also accepted
    /// (TikTok serialises some large integers as strings).
    /// </summary>
    /// <typeparam name="T">Any numeric type that implements <see cref="INumber{TSelf}"/>.</typeparam>
    /// <exception cref="TiktokParsingException">
    /// Thrown if the field is absent, not a number or numeric string, or the string
    /// cannot be parsed as <typeparamref name="T"/>.
    /// </exception>
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

    /// <summary>
    /// Returns the boolean value of <paramref name="field"/> in <paramref name="node"/>.
    /// </summary>
    /// <exception cref="TiktokParsingException">Thrown if the field is absent or not a JSON boolean.</exception>
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
