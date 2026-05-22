using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        var publicUrl = $"{_options.PublicBaseUrl.TrimEnd('/')}/{filename}";

        try
        {
            await _s3.GetObjectMetadataAsync(_options.BucketName, filename, ct);
            return publicUrl;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Archivo {Filename} no existe en R2, procediendo a subirlo", filename);
        }

        using var stream = new MemoryStream(data, writable: false);

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = filename,
            InputStream = stream,
            ContentType = "video/mp4",
            DisablePayloadSigning = true
        };

        await _s3.PutObjectAsync(request, ct);

        return publicUrl;
    }
}
