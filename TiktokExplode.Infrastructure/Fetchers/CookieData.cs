namespace TiktokExplode.Infrastructure.Fetchers;

public sealed record CookieData
{
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
}
