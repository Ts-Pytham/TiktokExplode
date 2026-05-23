namespace TiktokExplode.Bot.Configuration;

public class BotSettings
{
    public required string Token { get; init; }

    /// <summary>
    /// ID del servidor donde registrar los comandos al instante (desarrollo).
    /// Si es 0 o no está configurado, se registran globalmente (hasta 1 hora de propagación).
    /// </summary>
    public ulong GuildId { get; init; }
}
