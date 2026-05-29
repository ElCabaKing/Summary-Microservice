using SummaryService.Domain.Entities;

namespace SummaryService.Application.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByTenantIdAsync(string tenantId, CancellationToken ct);
    Task CreateAsync(Client client, CancellationToken ct);
    Task<int?> GetMaxTenantNumberAsync(CancellationToken ct);
    Task<List<string>> GetAllDomainsAsync(CancellationToken ct);
    Task<Client?> GetByDomainAsync(string domain, CancellationToken ct);
}
