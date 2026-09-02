using Mercato.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

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
        var secure = configuration.GetValue<bool>("ObjectStorage:Secure");

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

        source.Position = 0;
        using var image = await Image.LoadAsync(source, cancellationToken);
        if (image.Width > 10000 || image.Height > 10000)
            throw new InvalidOperationException("Product image dimensions are too large.");
        image.Mutate(x => x.AutoOrient());

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

        using var thumbnail = image.Clone(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(480, 480),
            Mode = ResizeMode.Max
        }));
        await using var thumbStream = new MemoryStream();
        await thumbnail.SaveAsync(thumbStream, new WebpEncoder { Quality = 82 }, cancellationToken);
        thumbStream.Position = 0;
        await _client.PutObjectAsync(new PutObjectArgs()
            .WithBucket(_bucket)
            .WithObject(thumbnailName)
            .WithStreamData(thumbStream)
            .WithObjectSize(thumbStream.Length)
            .WithContentType("image/webp"), cancellationToken);

        return new ProductMediaResult(
            $"/api/media/{Uri.EscapeDataString(originalName)}",
            $"/api/media/{Uri.EscapeDataString(thumbnailName)}");
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
                .WithCallbackStream(async source => await source.CopyToAsync(stream, cancellationToken)), cancellationToken);
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
