using System.Net.Http.Headers;

namespace TiktokExplode.Bot.CDN;

public sealed class ZeroXZeroCdnProvider(IHttpClientFactory httpClientFactory) : ICdnProvider
{
    public string Name => "0x0.st";

    public async Task<string> UploadAsync(byte[] data, string filename, CancellationToken ct = default)
    {
        var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TiktokExplodeBot/1.1");

        using var form = new MultipartFormDataContent();
        var fc = new ByteArrayContent(data);
        fc.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        form.Add(fc, "file", filename);

        var req = new HttpRequestMessage(HttpMethod.Post, "https://0x0.st") { Content = form };
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        return (await resp.Content.ReadAsStringAsync(ct)).Trim();
    }
}
