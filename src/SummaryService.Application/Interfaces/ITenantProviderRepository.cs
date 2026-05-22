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

    Task<IEnumerable<TenantProvider>> GetAllProvidersAsync(
        string tenantId,
        CancellationToken ct);

    Task DeleteProviderAsync(
        string tenantId,
        string provider,
        CancellationToken ct);
}
