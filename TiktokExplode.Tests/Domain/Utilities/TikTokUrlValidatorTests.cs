using FluentAssertions;
using TiktokExplode.Domain.Utilities;

namespace TiktokExplode.Tests.Domain.Utilities;

public class TikTokUrlValidatorTests
{
    [Theory]
    [InlineData("https://www.tiktok.com/@user/video/1234567890123456789",        "standard www video")]
    [InlineData("https://tiktok.com/@user/video/1234567890123456789",            "no-www video")]
    [InlineData("https://vm.tiktok.com/ZMkAbCdEf/",                             "vm short link")]
    [InlineData("https://vt.tiktok.com/ZMkAbCdEf/",                             "vt short link")]
    [InlineData("https://www.tiktok.com/@user/video/1234567890123456789?lang=en","with query string")]
    [InlineData("https://www.tiktok.com/@user/video/1234567890123456789#comments","with fragment")]
    [InlineData("https://www.tiktok.com/@user.name/video/1234567890123456789",   "username with dots")]
    [InlineData("https://www.tiktok.com/@user_123/video/1234567890123456789",    "username with underscores")]
    [InlineData("https://www.tiktok.com/@user/video/1234567890123456789?lang=en&source=share", "multiple query params")]
    public void Should_NotThrow_When_UrlIsValid(string url, string _)
    {
        var act = () => TikTokUrlValidator.Validate(url);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("http://www.tiktok.com/@user/video/123",  "plain HTTP")]
    [InlineData("ftp://www.tiktok.com/@user/video/123",   "FTP")]
    [InlineData("ws://www.tiktok.com/@user/video/123",    "WebSocket")]
    [InlineData("file:///www.tiktok.com/@user/video/123", "file scheme")]
    public void Should_ThrowArgumentException_When_SchemeIsNotHttps(string url, string _)
    {
        var act = () => TikTokUrlValidator.Validate(url);

        act.Should().ThrowExactly<ArgumentException>()
            .WithParameterName(nameof(url));
    }

    [Theory]
    [InlineData("https://youtube.com/watch?v=abc",                   "YouTube")]
    [InlineData("https://instagram.com/reel/abc",                    "Instagram")]
    [InlineData("https://twitter.com/user/status/123",               "Twitter")]
    [InlineData("https://tiktok.com.evil.com/@user/video/123",       "domain spoofing suffix")]
    [InlineData("https://eviltiktok.com/@user/video/123",            "domain spoofing prefix")]
    [InlineData("https://subdomain.tiktok.com/@user/video/123",      "unknown subdomain")]
    [InlineData("https://subdomain.www.tiktok.com/@user/video/123",  "double subdomain")]
    [InlineData("https://m.tiktok.com/@user/video/123",              "mobile subdomain (not registered)")]
    public void Should_ThrowArgumentException_When_HostIsNotRecognizedTikTokDomain(string url, string _)
    {
        var act = () => TikTokUrlValidator.Validate(url);

        act.Should().ThrowExactly<ArgumentException>()
            .WithParameterName(nameof(url));
    }

    [Theory]
    [InlineData("not-a-url",          "plain string")]
    [InlineData("/relative/path",     "relative path")]
    [InlineData("@user/video/123",    "relative tiktok-like path")]
    [InlineData("",                   "empty string")]
    [InlineData("   ",                "whitespace only")]
    public void Should_ThrowArgumentException_When_UrlIsNotAbsolute(string url, string _)
    {
        var act = () => TikTokUrlValidator.Validate(url);

        act.Should().ThrowExactly<ArgumentException>()
            .WithParameterName(nameof(url));
    }

    [Fact]
    public void Should_ThrowArgumentException_When_UrlIsNull()
    {
        var act = () => TikTokUrlValidator.Validate(null!);

        act.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("url");
    }
}
