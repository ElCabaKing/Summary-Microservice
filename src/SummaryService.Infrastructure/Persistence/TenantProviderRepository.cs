using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Entities;
using SummaryService.Domain.Options;

namespace SummaryService.Infrastructure.Persistence;

public sealed class TenantProviderRepository(
    IOptions<ConnectionStringsOptions> connectionStrings)
    : ITenantProviderRepository
{
    public async Task<TenantProvider?> GetActiveProviderAsync(
        string tenantId,
        string provider,
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionStrings.Value.Default);

        return await conn.QueryFirstOrDefaultAsync<TenantProvider>(
            new CommandDefinition(
                """
                SELECT
                    Id,
                    TenantId,
                    Provider,
                    EncryptedApiKey,
                    IsActive,
                    CreatedAt,
                    UpdatedAt
                FROM TenantProviders
                WHERE TenantId = @TenantId
                  AND Provider = @Provider
                  AND IsActive = 1
                """,
                new { TenantId = tenantId, Provider = provider },
                cancellationToken: ct));
    }

    public async Task AddProviderAsync(
        TenantProvider tenantProvider,
        CancellationToken ct)
    {
        await using var conn = new SqlConnection(connectionStrings.Value.Default);

        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                MERGE TenantProviders AS target
                USING (SELECT @TenantId AS TenantId, @Provider AS Provider) AS source
                ON target.TenantId = source.TenantId AND target.Provider = source.Provider
                WHEN MATCHED THEN
                    UPDATE SET
                        EncryptedApiKey = @EncryptedApiKey,
                        IsActive = @IsActive,
                        UpdatedAt = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (Id, TenantId, Provider, EncryptedApiKey, IsActive, CreatedAt)
                    VALUES (@Id, @TenantId, @Provider, @EncryptedApiKey, @IsActive, SYSUTCDATETIME());
                """,
                new
                {
                    tenantProvider.Id,
                    tenantProvider.TenantId,
                    tenantProvider.Provider,
                    tenantProvider.EncryptedApiKey,
                    tenantProvider.IsActive
                },
                cancellationToken: ct));
    }
}
