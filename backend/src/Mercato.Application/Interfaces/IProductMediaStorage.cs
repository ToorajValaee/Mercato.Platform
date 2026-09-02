namespace Mercato.Application.Interfaces;

public sealed record ProductMediaResult(string ImageUrl, string ThumbnailUrl);
public sealed record StoredMedia(Stream Content, string ContentType);

public interface IProductMediaStorage
{
    Task<ProductMediaResult> SaveProductImageAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<StoredMedia?> OpenAsync(string objectName, CancellationToken cancellationToken = default);
}
