using SummaryService.Domain.Entities;

namespace SummaryService.Application.Interfaces;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct);

    Task CreateAsync(ApiKey apiKey, CancellationToken ct);
}
