using Mercato.NopCommerce.Core;

namespace Mercato.Connector.Plugin;

public sealed class ConnectorPluginCore
{
    private readonly MercatoApiClient _client;

    public ConnectorPluginCore(MercatoApiClient client)
    {
        _client = client;
    }

    public Task<bool> CheckConnectionAsync(CancellationToken cancellationToken = default)
        => _client.HealthAsync(cancellationToken);
}
