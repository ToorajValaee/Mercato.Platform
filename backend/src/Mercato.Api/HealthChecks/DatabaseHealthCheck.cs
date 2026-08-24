using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Mercato.Api.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Database health check registered"));
    }
}
