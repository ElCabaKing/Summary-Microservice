using SummaryService.Domain.Entities;

namespace SummaryService.Application.Interfaces;

public interface ITenantProviderRepository
{
    Task<TenantProvider?> GetActiveProviderAsync(
        string tenantId,
        string provider,
        CancellationToken ct);

    Task AddProviderAsync(
        TenantProvider tenantProvider,
        CancellationToken ct);
}
