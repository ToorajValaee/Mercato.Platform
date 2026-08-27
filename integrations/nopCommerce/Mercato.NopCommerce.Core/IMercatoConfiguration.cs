namespace Mercato.NopCommerce.Core;

public interface IMercatoConfiguration
{
    string BaseUrl { get; }
    string BearerToken { get; }
    Guid? DefaultBranchId { get; }
}
