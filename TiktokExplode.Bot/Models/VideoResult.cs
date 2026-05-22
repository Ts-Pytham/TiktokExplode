using TiktokExplode.Domain.Entities;

namespace TiktokExplode.Bot.Models;

public sealed class VideoResult
{
    public required string Url { get; set; }
    public required Video Video { get; set; }
}
