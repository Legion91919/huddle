using Huddle.SharedKernel.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Huddle.Api.Infrastructure;

/// <summary>Ready only when the ledger balances: every posting sums to zero and every balance equals its entries.</summary>
internal sealed class LedgerIntegrityHealthCheck(ILedger ledger) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var integrity = ledger.CheckIntegrity();
        return Task.FromResult(integrity.Balanced
            ? HealthCheckResult.Healthy($"{integrity.Postings} postings balanced")
            : HealthCheckResult.Unhealthy("Ledger out of balance"));
    }
}
