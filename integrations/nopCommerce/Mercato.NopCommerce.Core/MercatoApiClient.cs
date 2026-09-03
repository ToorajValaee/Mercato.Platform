using System.Net.Http.Json;
using System.Text.Json;

namespace Mercato.NopCommerce.Core;

public sealed record MercatoConnectorOptions(string BaseUrl, string BearerToken);
public sealed record MercatoBranch(Guid Id, string Name, string? Address);
public sealed record CatalogProduct(Guid ProductId, string Name, string? Sku, string? ImageUrl, decimal SalePrice, Guid? CategoryId, string? CategoryName, Guid? ArtistId, Guid? BranchId, int? AvailableQuantity)
{
    public string NopSku => string.IsNullOrWhiteSpace(Sku) ? $"MERCATO-{ProductId:N}" : Sku.Trim();
}
public sealed record CommerceOrderItem(Guid ProductId, int Quantity);
public sealed record CommerceOrder(string ExternalOrderId, Guid BranchId, Guid CustomerId, string PaymentMethod, IReadOnlyList<CommerceOrderItem> Items);

public sealed class MercatoApiClient
{
    private const string MissingBaseUrlMessage = "Mercato Base URL is not configured. Configure the Mercato Connector plugin or set Mercato:BaseUrl.";
    private readonly HttpClient _http;
    private readonly string? _configurationError;

    public MercatoApiClient(HttpClient http, MercatoConnectorOptions options)
    {
        _http = http;

        var baseUrl = options.BaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _configurationError = MissingBaseUrlMessage;
        }
        else if (!Uri.TryCreate(baseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var baseUri) ||
                 (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            _configurationError = "Mercato Base URL must be an absolute HTTP or HTTPS URL.";
        }
        else
        {
            _http.BaseAddress = baseUri;
        }

        if (!string.IsNullOrWhiteSpace(options.BearerToken))
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.BearerToken);
    }

    public bool IsConfigured => _configurationError is null && _http.BaseAddress is not null;

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return (await _http.GetAsync("health", cancellationToken)).IsSuccessStatusCode;
    }

    public async Task<IReadOnlyList<MercatoBranch>> GetBranchesAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        return await _http.GetFromJsonAsync<List<MercatoBranch>>("api/branches", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<CatalogProduct>> GetCatalogAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var url = branchId is Guid id && id != Guid.Empty ? $"api/catalog?branchId={id}" : "api/catalog";
        return await _http.GetFromJsonAsync<List<CatalogProduct>>(url, cancellationToken) ?? [];
    }

    public async Task<JsonElement> SyncOrderAsync(CommerceOrder order, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var externalOrderId = order.ExternalOrderId.Trim();
        var request = new
        {
            order.BranchId,
            order.CustomerId,
            PaymentMethod = order.PaymentMethod,
            IdempotencyKey = $"nop:{externalOrderId}",
            Items = order.Items.Select(x => new { x.ProductId, x.Quantity })
        };
        using var response = await _http.PostAsJsonAsync("api/pos/checkout", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException(_configurationError ?? MissingBaseUrlMessage);
    }
}
