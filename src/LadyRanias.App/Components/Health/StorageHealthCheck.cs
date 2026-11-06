using Azure.Data.Tables;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LadyRanias.App.Components.Health;

internal sealed class StorageHealthCheck(TableServiceClient tableServiceClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var _ in tableServiceClient.QueryAsync(maxPerPage: 1, cancellationToken: cancellationToken))
            {
                break;
            }

            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Får ikke kontakt med Table Storage", ex);
        }
    }
}
