using Nop.Web.Framework.Models;

namespace Mercato.Connector.Plugin;

public sealed record MercatoConnectorConfigurationModel : BaseNopModel
{
    public string BaseUrl { get; set; } = string.Empty;
    public string BearerToken { get; set; } = string.Empty;
    public string DefaultBranchId { get; set; } = string.Empty;
}
