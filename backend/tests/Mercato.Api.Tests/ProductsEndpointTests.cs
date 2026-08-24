using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mercato.Api.Tests;

public class ProductsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProductsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Products_ReturnsSuccessfulResponse()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                    response.StatusCode == HttpStatusCode.NoContent);
    }
}
