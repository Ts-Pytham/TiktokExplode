using TiktokExplode.Domain.ValueObjects.Authors;

namespace TiktokExplode.Domain.Entities;

public sealed class Author
{
    public string Id { get; init; } = string.Empty;
    public string UniqueId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsVerified { get; init; }
    public bool IsPrivate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ProfileImageVariants Avatar { get; init; } = new();
    public AuthorStats Stats { get; init; } = new();
}
