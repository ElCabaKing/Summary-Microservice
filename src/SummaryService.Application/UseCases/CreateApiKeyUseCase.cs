using System.Security.Cryptography;
using System.Text;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Entities;
using SummaryService.Shared.Models;

namespace SummaryService.Application.UseCases;

public sealed class CreateApiKeyUseCase(
    IApiKeyRepository repository)
{
    public async Task<Result<ApiKeyResult>> ExecuteAsync(
        string tenantId,
        string role,
        CancellationToken ct)
    {
        try
        {
            var plainKey = GenerateApiKey();
            var keyHash = ComputeHash(plainKey);
            var keyPrefix = plainKey[..8];

            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                KeyHash = keyHash,
                KeyPrefix = keyPrefix,
                TenantId = tenantId,
                Role = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await repository.CreateAsync(apiKey, ct);

            return Result<ApiKeyResult>.Success(new ApiKeyResult
            {
                ApiKey = plainKey,
                TenantId = tenantId,
                Role = role
            });
        }
        catch (Exception ex)
        {
            return Result<ApiKeyResult>.Failure(
                new Error("APIKEY_CREATE_ERROR", ex.Message));
        }
    }

    private static string GenerateApiKey()
    {
        const string prefix = "sk_live_";
        var bytes = RandomNumberGenerator.GetBytes(32);
        return prefix + Convert.ToHexStringLower(bytes);
    }

    private static string ComputeHash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(bytes);
    }
}

public sealed class ApiKeyResult
{
    public string ApiKey { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
