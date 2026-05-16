namespace TiktokExplode.Infrastructure.Options;

public sealed class TikTokOptions
{
    // Delay entre warmup y la request real
    public TimeSpan RequestDelay { get; set; } = TimeSpan.FromMilliseconds(1200);
    
    // Reintentos automáticos si detecta WAF
    public int MaxWafRetries { get; set; } = 3;
    
    // Delay entre reintentos (crece exponencialmente internamente)
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(2);
}