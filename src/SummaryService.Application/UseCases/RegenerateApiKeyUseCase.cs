using SummaryService.Application.DTOs;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Entities;
using SummaryService.Shared.Models;

namespace SummaryService.Application.UseCases;

public sealed class RegenerateApiKeyUseCase(
    IClientRepository clientRepository,
    IApiKeyRepository apiKeyRepository,
    IApiKeyHashService hashService)
{
    public async Task<Result<ClientKeyResult>> ExecuteAsync(
        string tenantId,
        CancellationToken ct)
    {
        try
        {
            var client = await clientRepository.GetByTenantIdAsync(tenantId, ct).ConfigureAwait(false);

            if (client is null)
                return Result<ClientKeyResult>.Failure(
                    new Error("CLIENT_NOT_FOUND", $"No client found with tenantId '{tenantId}'"));

            await apiKeyRepository.DeactivateByTenantIdAsync(tenantId, ct).ConfigureAwait(false);

            var plainKey = hashService.GenerateApiKey();
            var keyHash = hashService.ComputeHash(plainKey);

            var newApiKey = ApiKey.Create(keyHash, tenantId);

            await apiKeyRepository.CreateAsync(newApiKey, ct).ConfigureAwait(false);

            return Result<ClientKeyResult>.Success(new ClientKeyResult
            {
                ApiKey = plainKey,
                TenantId = tenantId,
                CompanyName = client.CompanyName
            });
        }
        catch (Exception ex)
        {
            return Result<ClientKeyResult>.Failure(
                new Error("REGENERATE_ERROR", ex.Message));
        }
    }
}
