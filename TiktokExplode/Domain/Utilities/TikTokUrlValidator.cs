namespace TiktokExplode.Domain.Utilities;

/// <summary>
/// Validates TikTok video URLs before they are used for network requests.
/// </summary>
public static class TiktokUrlValidator
{
    /// <summary>
    /// Set of recognized TikTok hostnames, including short-link variants
    /// (<c>vm.tiktok.com</c>, <c>vt.tiktok.com</c>).
    /// </summary>
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
    /// <param name="url">The URL string to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the URL is not a valid absolute HTTPS TikTok URL.</exception>
    public static void Validate(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException($"'{url}' is not a valid absolute HTTPS URI.", nameof(url));

        if (!_validHosts.Contains(uri.Host))
            throw new ArgumentException($"'{uri.Host}' is not a recognized TikTok domain.", nameof(url));
    }
}
