using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TiktokExplode.Bot.Configuration;

namespace TiktokExplode.Bot.CDN;

public sealed class CloudflareR2CdnProvider : ICdnProvider
{
    public string Name => "Cloudflare R2";

    private readonly AmazonS3Client _s3;
    private readonly CloudflareR2Options _options;
    private readonly ILogger _logger;

    public CloudflareR2CdnProvider(IOptions<CloudflareR2Options> options, ILogger<CloudflareR2CdnProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            AuthenticationRegion = "auto"
        };

        _s3 = new AmazonS3Client(
            new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey),
            config);
    }

    public async Task<string> UploadAsync(byte[] data, string filename, CancellationToken ct = default)
    {
        if (await GetUrlAsync(filename, ct) is string existingUrl && !string.IsNullOrWhiteSpace(existingUrl))
        {
            return existingUrl;
        }

        var key = $"{filename}.mp4";
        _logger.LogInformation("Subiendo archivo {Filename} a R2", key);

        using var stream = new MemoryStream(data, writable: false);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = stream,
            ContentType = "video/mp4",
            DisablePayloadSigning = true
        };

        await _s3.PutObjectAsync(request, ct);

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
    }

    public async Task<string> GetUrlAsync(string filename, CancellationToken ct = default)
    {
        var key = $"{filename}.mp4";
        var publicUrl = $"{_options.PublicBaseUrl.TrimEnd('/')}/{key}";
        try
        {
            await _s3.GetObjectMetadataAsync(_options.BucketName, key, ct);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Archivo {Filename} no encontrado en R2", key);
            return string.Empty;
        }

        return publicUrl;
    }
}
