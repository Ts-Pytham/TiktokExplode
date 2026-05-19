namespace TiktokExplode.Infrastructure.Fetchers;

public sealed record PageFetchResult
{
    public required string HtmlContent { get; init; }
    public required IReadOnlyList<CookieData> Cookies { get; init; }
}