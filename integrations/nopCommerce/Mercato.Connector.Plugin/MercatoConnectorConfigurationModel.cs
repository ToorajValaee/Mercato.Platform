using Nop.Web.Framework.Models;

namespace Mercato.Connector.Plugin;

public sealed record MercatoConnectorConfigurationModel : BaseNopModel
{
    public string BaseUrl { get; init; } = string.Empty;
    public string BearerToken { get; init; } = string.Empty;
    public string DefaultBranchId { get; init; } = string.Empty;
}
