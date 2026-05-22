using System.Net.Http.Headers;

namespace TiktokExplode.Bot.CDN;

public sealed class LitterboxCdnProvider(IHttpClientFactory httpClientFactory) : ICdnProvider
{
    public string Name => "Litterbox (catbox.moe)";

    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task<string> UploadAsync(byte[] data, string filename, CancellationToken ct = default)
    {
        var http = _httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TiktokExplodeBot/1.1");

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent("fileupload"), "reqtype");
        form.Add(new StringContent("72h"), "time");
        var fc = new ByteArrayContent(data);
        fc.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
        form.Add(fc, "fileToUpload", filename);

        var req = new HttpRequestMessage(HttpMethod.Post,
            "https://litterbox.catbox.moe/resources/internals/api.php") { Content = form };
        using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        return (await resp.Content.ReadAsStringAsync(ct)).Trim();
    }
}
