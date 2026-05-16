namespace TiktokExplode.Domain.Utilities;

public static class TikTokUrlValidator
{
    private static readonly HashSet<string> _validHosts =
    [
        "tiktok.com",
        "www.tiktok.com",
        "vm.tiktok.com",
        "vt.tiktok.com"
    ];

    /// <summary>
    /// Validates that <paramref name="url"/> is a valid TikTok URL.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the URL is not a valid TikTok URL.</exception>
    public static void Validate(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"'{url}' is not a valid absolute HTTPS URI.", nameof(url));

        if (!_validHosts.Contains(uri.Host))
            throw new ArgumentException($"'{uri.Host}' is not a recognized TikTok domain.", nameof(url));
    }
}
