using Dapper;
using Microsoft.Extensions.Options;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Entities;
using SummaryService.Domain.Options;

namespace SummaryService.Infrastructure.Persistence;

public sealed class ApiKeyRepository(
    IOptions<ConnectionStringsOptions> connectionStrings)
    : BaseRepository(connectionStrings), IApiKeyRepository
{
    public async Task<IEnumerable<ApiKey>> GetByPrefixAsync(
        string prefix,
        CancellationToken ct)
    {
        await using var conn = GetConnection();

        return await conn.QueryAsync<ApiKey>(
            new CommandDefinition(
                """
                SELECT
                    Id,
                    KeyHash,
                    KeyPrefix,
                    TenantId,
                    Role,
                    IsActive,
                    CreatedAt,
                    UpdatedAt
                FROM ApiKeys
                WHERE KeyPrefix = @Prefix
                  AND IsActive = 1
                """,
                new { Prefix = prefix },
                cancellationToken: ct));
    }

    public async Task DeactivateByTenantIdAsync(
        string tenantId,
        CancellationToken ct)
    {
        await using var conn = GetConnection();

        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                UPDATE ApiKeys
                SET IsActive = 0, UpdatedAt = SYSUTCDATETIME()
                WHERE TenantId = @TenantId AND IsActive = 1
                """,
                new { TenantId = tenantId },
                cancellationToken: ct));
    }

    public async Task CreateAsync(
        ApiKey apiKey,
        CancellationToken ct)
    {
        await using var conn = GetConnection();

        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO ApiKeys (Id, KeyHash, KeyPrefix, TenantId, Role, IsActive, CreatedAt)
                VALUES (@Id, @KeyHash, @KeyPrefix, @TenantId, @Role, @IsActive, @CreatedAt)
                """,
                new
                {
                    apiKey.Id,
                    apiKey.KeyHash,
                    apiKey.KeyPrefix,
                    apiKey.TenantId,
                    apiKey.Role,
                    apiKey.IsActive,
                    apiKey.CreatedAt
                },
                cancellationToken: ct));
    }
}
