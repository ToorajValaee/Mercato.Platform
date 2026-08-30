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
                var migrations = context.Database.GetMigrations();
                if (migrations.Any())
                    context.Database.Migrate();
                else
                    context.Database.EnsureCreated();

                // EnsureCreated does not evolve an existing schema. Keep this additive table
                // bootstrap until the production baseline migration is introduced.
                context.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS "UserBranchAssignments" (
                        "UserId" uuid NOT NULL,
                        "BranchId" uuid NOT NULL,
                        CONSTRAINT "PK_UserBranchAssignments" PRIMARY KEY ("UserId", "BranchId"),
                        CONSTRAINT "FK_UserBranchAssignments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                        CONSTRAINT "FK_UserBranchAssignments_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS "IX_UserBranchAssignments_BranchId" ON "UserBranchAssignments" ("BranchId");
                    """);

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
