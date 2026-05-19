namespace TiktokExplode.Infrastructure.Options;

public sealed class TikTokOptions
{
    // Reintentos automáticos si detecta WAF
    public int MaxWafRetries { get; set; } = 3;
    
    // Delay entre reintentos (crece exponencialmente internamente)
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);
}