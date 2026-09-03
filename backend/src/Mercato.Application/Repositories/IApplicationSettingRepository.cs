namespace Mercato.Application.Repositories;

public interface IApplicationSettingRepository
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
    Task<bool> GetBooleanAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default);
}
