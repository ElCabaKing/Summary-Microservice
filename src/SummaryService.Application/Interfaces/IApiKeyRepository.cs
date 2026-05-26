using SummaryService.Domain.Entities;

namespace SummaryService.Application.Interfaces;

public interface IApiKeyRepository
{
    Task<IEnumerable<ApiKey>> GetByPrefixAsync(string prefix, CancellationToken ct);

    Task CreateAsync(ApiKey apiKey, CancellationToken ct);

    Task DeactivateByTenantIdAsync(string tenantId, CancellationToken ct);
}
