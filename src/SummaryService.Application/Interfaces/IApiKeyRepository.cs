using SummaryService.Domain.Entities;

namespace SummaryService.Application.Interfaces;

public interface IApiKeyRepository
{
    Task<IEnumerable<ApiKey>> GetByTenantIdAsync(string tenantId, CancellationToken ct);

    Task CreateAsync(ApiKey apiKey, CancellationToken ct);

    Task DeactivateByTenantIdAsync(string tenantId, CancellationToken ct);
}
