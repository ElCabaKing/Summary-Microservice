using SummaryService.Application.DTOs;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Entities;
using SummaryService.Shared.Models;

namespace SummaryService.Application.UseCases;

public sealed class RegisterClientUseCase(
    IClientRepository clientRepository,
    IApiKeyRepository apiKeyRepository,
    IApiKeyHashService hashService)
{
    public async Task<Result<ClientKeyResult>> ExecuteAsync(
        string companyName,
        string? email,
        string? contactName,
        string domain,
        CancellationToken ct)
    {
        try
        {
            var maxNumber = await clientRepository.GetMaxTenantNumberAsync(ct).ConfigureAwait(false);
            var nextNumber = (maxNumber ?? 0) + 1;
            var tenantId = $"tenant_{nextNumber}";

            var client = new Client
            {
                Id = Guid.NewGuid(),
                CompanyName = companyName,
                Email = email,
                ContactName = contactName,
                TenantId = tenantId,
                Domain = domain,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var plainKey = hashService.GenerateApiKey();
            var keyHash = hashService.ComputeHash(plainKey);

            var apiKey = ApiKey.Create(keyHash, tenantId);

            await clientRepository.CreateAsync(client, ct).ConfigureAwait(false);
            await apiKeyRepository.CreateAsync(apiKey, ct).ConfigureAwait(false);

            return Result<ClientKeyResult>.Success(new ClientKeyResult
            {
                ApiKey = plainKey,
                TenantId = tenantId,
                CompanyName = companyName
            });
        }
        catch (Exception ex)
        {
            return Result<ClientKeyResult>.Failure(
                new Error("REGISTER_ERROR", ex.Message));
        }
    }
}
