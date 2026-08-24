using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Mercato.Api.Tests;

public class SecurityEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SecurityEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Products_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
