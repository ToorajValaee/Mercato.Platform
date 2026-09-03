using Mercato.Application.Repositories;
using Mercato.Domain.Entities;
using Mercato.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Mercato.Infrastructure.Repositories;

public sealed class ApplicationSettingRepository : IApplicationSettingRepository
{
    private readonly MercatoDbContext _db;

    public ApplicationSettingRepository(MercatoDbContext db) => _db = db;

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        => await _db.ApplicationSettings.AsNoTracking()
            .Where(x => x.Key == key)
            .Select(x => x.Value)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var row = await _db.ApplicationSettings.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
        if (row is null) _db.ApplicationSettings.Add(new ApplicationSetting { Key = key, Value = value });
        else row.Value = value;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> GetBooleanAsync(string key, bool defaultValue = false, CancellationToken cancellationToken = default)
    {
        var value = await GetAsync(key, cancellationToken);
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
