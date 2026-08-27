using Nop.Core.Configuration;

namespace Mercato.Connector.Plugin;

public sealed class MercatoConnectorSettings : ISettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    public string DefaultBranchId { get; set; } = string.Empty;
}
