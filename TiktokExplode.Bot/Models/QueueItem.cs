using TiktokExplode.Domain.Entities;

namespace TiktokExplode.Bot.Models;

public sealed record QueueItem(string Url, string OriginalUrl, Video Video);