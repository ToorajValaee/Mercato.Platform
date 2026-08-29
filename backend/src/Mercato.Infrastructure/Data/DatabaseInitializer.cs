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
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Database creation/migration runs once during application startup. Use the
                // provider's synchronous connection path here deliberately: on constrained
                // hosted runners the async DNS worker can fail with SocketError.TryAgain even
                // for a literal loopback address. Request-time database operations remain async.
                var migrations = context.Database.GetMigrations();
                if (migrations.Any())
                    context.Database.Migrate();
                else
                    context.Database.EnsureCreated();

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
