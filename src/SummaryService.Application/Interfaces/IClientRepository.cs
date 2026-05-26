using SummaryService.Domain.Entities;

namespace SummaryService.Application.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByTenantIdAsync(string tenantId, CancellationToken ct);
    Task CreateAsync(Client client, CancellationToken ct);
    Task<int?> GetMaxTenantNumberAsync(CancellationToken ct);
}
