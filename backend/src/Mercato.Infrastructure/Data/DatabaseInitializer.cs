using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mercato.Infrastructure.Data;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 5;

    public static async Task InitializeAsync(MercatoDbContext context, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                var migrations = context.Database.GetMigrations();
                if (migrations.Any())
                    await context.Database.MigrateAsync(cancellationToken);
                else
                    await context.Database.EnsureCreatedAsync(cancellationToken);

                return;
            }
            catch (Exception exception) when (
                attempt < MaxAttempts &&
                IsTransient(exception) &&
                !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception exception)
    {
        if (exception is TimeoutException)
            return true;

        if (exception is NpgsqlException npgsqlException && npgsqlException.IsTransient)
            return true;

        return exception.InnerException is not null && IsTransient(exception.InnerException);
    }
}
