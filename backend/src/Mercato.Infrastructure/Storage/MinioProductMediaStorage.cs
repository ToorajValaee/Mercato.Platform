using Mercato.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using SkiaSharp;

namespace Mercato.Infrastructure.Storage;

public sealed class MinioProductMediaStorage : IProductMediaStorage
{
    private const long MaxUploadBytes = 12 * 1024 * 1024;
    private readonly IMinioClient _client;
    private readonly string _bucket;
    private readonly SemaphoreSlim _bucketLock = new(1, 1);
    private bool _bucketReady;

    public MinioProductMediaStorage(IConfiguration configuration)
    {
        var endpoint = configuration["ObjectStorage:Endpoint"] ?? "minio:9000";
        var accessKey = configuration["ObjectStorage:AccessKey"] ?? "mercato";
        var secretKey = configuration["ObjectStorage:SecretKey"] ?? "MercatoMinio123!";
        _bucket = configuration["ObjectStorage:Bucket"] ?? "mercato-media";
        var secure = bool.TryParse(configuration["ObjectStorage:Secure"], out var configuredSecure) && configuredSecure;

        _client = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .WithSSL(secure)
            .Build();
    }

    public async Task<ProductMediaResult> SaveProductImageAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!content.CanRead) throw new InvalidOperationException("Image stream is not readable.");
        await using var source = new MemoryStream();
        await content.CopyToAsync(source, cancellationToken);
        if (source.Length == 0 || source.Length > MaxUploadBytes)
            throw new InvalidOperationException("Product image must be between 1 byte and 12 MB.");

        var bytes = source.ToArray();
        using var bitmap = SKBitmap.Decode(bytes) ?? throw new InvalidOperationException("The uploaded file is not a supported image.");
        if (bitmap.Width <= 0 || bitmap.Height <= 0 || bitmap.Width > 10000 || bitmap.Height > 10000)
            throw new InvalidOperationException("Product image dimensions are invalid or too large.");

        await EnsureBucketAsync(cancellationToken);
        var id = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp")) extension = ".bin";
        var originalName = $"products/{id}/original{extension}";
        var thumbnailName = $"products/{id}/thumbnail.webp";

        source.Position = 0;
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(originalName)
            .WithStreamData(source)
            .WithObjectSize(source.Length)
            .WithContentType(string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType), cancellationToken);

        var scale = Math.Min(1d, Math.Min(480d / bitmap.Width, 480d / bitmap.Height));
        var width = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
        var height = Math.Max(1, (int)Math.Round(bitmap.Height * scale));
        using var thumbnail = bitmap.Resize(
            new SKImageInfo(width, height, bitmap.ColorType, bitmap.AlphaType),
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            ?? throw new InvalidOperationException("Could not generate product thumbnail.");
        using var thumbnailImage = SKImage.FromBitmap(thumbnail);
        using var encoded = thumbnailImage.Encode(SKEncodedImageFormat.Webp, 82)
            ?? throw new InvalidOperationException("Could not encode product thumbnail.");
        await using var thumbStream = new MemoryStream();
        encoded.SaveTo(thumbStream);
        thumbStream.Position = 0;
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(thumbnailName)
            .WithStreamData(thumbStream)
            .WithObjectSize(thumbStream.Length)
            .WithContentType("image/webp"), cancellationToken);

        return new ProductMediaResult(
            $"/api/media/{originalName}",
            $"/api/media/{thumbnailName}");
    }

    public async Task<StoredMedia?> OpenAsync(string objectName, CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);
        var stream = new MemoryStream();
        try
        {
            var stat = await _client.StatObjectAsync(new StatObjectArgs().WithBucket(_bucket).WithObject(objectName), cancellationToken);
            await _client.GetObjectAsync(new GetObjectArgs()
                .WithBucket(_bucket)
                .WithObject(objectName)
                .WithCallbackStream(source => source.CopyTo(stream)), cancellationToken);
            stream.Position = 0;
            return new StoredMedia(stream, stat.ContentType ?? "application/octet-stream");
        }
        catch
        {
            await stream.DisposeAsync();
            return null;
        }
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        if (_bucketReady) return;
        await _bucketLock.WaitAsync(cancellationToken);
        try
        {
            if (_bucketReady) return;
            var exists = await _client.BucketExistsAsync(new BucketExistsArgs().WithBucket(_bucket), cancellationToken);
            if (!exists)
                await _client.MakeBucketAsync(new MakeBucketArgs().WithBucket(_bucket), cancellationToken);
            _bucketReady = true;
        }
        finally
        {
            _bucketLock.Release();
        }
    }
}
