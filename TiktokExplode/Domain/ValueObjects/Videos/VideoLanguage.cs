namespace TiktokExplode.Domain.ValueObjects.Videos;

public sealed record VideoLanguage
{
    public string PrimaryLanguage { get; init; } = string.Empty;
    public bool IsTranslatable { get; init; }
}
