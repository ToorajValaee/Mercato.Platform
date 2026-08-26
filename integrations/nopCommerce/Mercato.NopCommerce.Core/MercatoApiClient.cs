using System.Net.Http.Json;
using System.Text.Json;

namespace Mercato.NopCommerce.Core;

public sealed record MercatoConnectorOptions(string BaseUrl, string BearerToken);
public sealed record MercatoBranch(Guid Id, string Name, string? Address);
public sealed record CatalogProduct(Guid ProductId, string Name, string? Sku, decimal SalePrice, Guid? CategoryId, Guid? ArtistId, Guid? BranchId, int? AvailableQuantity)
{
    public string NopSku => string.IsNullOrWhiteSpace(Sku) ? $"MERCATO-{ProductId:N}" : Sku.Trim();
}
public sealed record CommerceOrderItem(Guid ProductId, int Quantity);
public sealed record CommerceOrder(string ExternalOrderId, Guid BranchId, Guid CustomerId, string PaymentMethod, IReadOnlyList<CommerceOrderItem> Items);

public sealed class MercatoApiClient
{
    private readonly HttpClient _http;

    public MercatoApiClient(HttpClient http, MercatoConnectorOptions options)
    {
        _http = http;
        _http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrWhiteSpace(options.BearerToken))
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.BearerToken);
    }

    public async Task<bool> HealthAsync(CancellationToken cancellationToken = default)
        => (await _http.GetAsync("health", cancellationToken)).IsSuccessStatusCode;

    public async Task<IReadOnlyList<MercatoBranch>> GetBranchesAsync(CancellationToken cancellationToken = default)
        => await _http.GetFromJsonAsync<List<MercatoBranch>>("api/branches", cancellationToken) ?? [];

    public async Task<IReadOnlyList<CatalogProduct>> GetCatalogAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var url = branchId is Guid id && id != Guid.Empty ? $"api/catalog?branchId={id}" : "api/catalog";
        return await _http.GetFromJsonAsync<List<CatalogProduct>>(url, cancellationToken) ?? [];
    }

    public async Task<JsonElement> SyncOrderAsync(CommerceOrder order, CancellationToken cancellationToken = default)
    {
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
}
