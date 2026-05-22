using SummaryService.Application.Interfaces;
using SummaryService.Application.Models;
using SummaryService.Domain.Entities;
using SummaryService.Shared.Models;

namespace SummaryService.Application.UseCases;

public sealed class ConfigureTenantProviderUseCase(
    ITenantProviderRepository repository,
    IAesEncryptionService encryptionService)
{
    public async Task<Result> ExecuteAsync(
        string tenantId,
        ConfigureTenantRequest request,
        CancellationToken ct)
    {
        try
        {
            var encryptedKey = encryptionService.Encrypt(request.ApiKey);

            var tenantProvider = new TenantProvider
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Provider = request.Provider,
                EncryptedApiKey = encryptedKey,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await repository.AddProviderAsync(tenantProvider, ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(
                new Error("CONFIG_ERROR", ex.Message));
        }
    }
}
