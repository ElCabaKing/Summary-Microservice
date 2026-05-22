using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Entities;
using SummaryService.Domain.Options;

namespace SummaryService.Infrastructure.Persistence;

public sealed class ApiKeyRepository(
    IOptions<ConnectionStringsOptions> connectionStrings)
    : IApiKeyRepository
{
    public async Task<ApiKey?> GetByHashAsync(
        string keyHash,
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionStrings.Value.Default);

        return await conn.QueryFirstOrDefaultAsync<ApiKey>(
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
                WHERE KeyHash = @KeyHash
                  AND IsActive = 1
                """,
                new { KeyHash = keyHash },
                cancellationToken: ct));
    }

    public async Task CreateAsync(
        ApiKey apiKey,
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionStrings.Value.Default);

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
