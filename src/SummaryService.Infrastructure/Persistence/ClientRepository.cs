using Dapper;
using Microsoft.Extensions.Options;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Entities;
using SummaryService.Domain.Options;

namespace SummaryService.Infrastructure.Persistence;

public sealed class ClientRepository(
    IOptions<ConnectionStringsOptions> connectionStrings)
    : BaseRepository(connectionStrings), IClientRepository
{
    public async Task<Client?> GetByTenantIdAsync(
        string tenantId,
        CancellationToken ct)
    {
        await using var conn = GetConnection();

        return await conn.QueryFirstOrDefaultAsync<Client>(
            new CommandDefinition(
                """
                SELECT
                    Id,
                    CompanyName,
                    Email,
                    ContactName,
                    TenantId,
                    Domain,
                    IsActive,
                    CreatedAt,
                    UpdatedAt
                FROM Clients
                WHERE TenantId = @TenantId
                """,
                new { TenantId = tenantId },
                cancellationToken: ct));
    }

    public async Task CreateAsync(
        Client client,
        CancellationToken ct)
    {
        await using var conn = GetConnection();

        await conn.ExecuteAsync(
            new CommandDefinition(
                """
                INSERT INTO Clients (Id, CompanyName, Email, ContactName, TenantId, Domain, IsActive, CreatedAt)
                VALUES (@Id, @CompanyName, @Email, @ContactName, @TenantId, @Domain, @IsActive, @CreatedAt)
                """,
                new
                {
                    client.Id,
                    client.CompanyName,
                    client.Email,
                    client.ContactName,
                    client.TenantId,
                    client.Domain,
                    client.IsActive,
                    client.CreatedAt
                },
                cancellationToken: ct));
    }

    public async Task<int?> GetMaxTenantNumberAsync(CancellationToken ct)
    {
        await using var conn = GetConnection();

        return await conn.QueryFirstOrDefaultAsync<int?>(
            new CommandDefinition(
                """
                SELECT MAX(CAST(SUBSTRING(TenantId, 8, LEN(TenantId)) AS INT))
                FROM Clients
                """,
                cancellationToken: ct));
    }

    public async Task<Client?> GetByDomainAsync(
        string domain,
        CancellationToken ct)
    {
        await using var conn = GetConnection();

        return await conn.QueryFirstOrDefaultAsync<Client>(
            new CommandDefinition(
                """
                SELECT
                    Id,
                    CompanyName,
                    Email,
                    ContactName,
                    TenantId,
                    Domain,
                    IsActive,
                    CreatedAt,
                    UpdatedAt
                FROM Clients
                WHERE Domain = @Domain
                  AND IsActive = 1
                """,
                new { Domain = domain },
                cancellationToken: ct));
    }

    public async Task<List<string>> GetAllDomainsAsync(CancellationToken ct)
    {
        await using var conn = GetConnection();

        var domains = await conn.QueryAsync<string>(
            new CommandDefinition(
                """
                SELECT Domain
                FROM Clients
                WHERE IsActive = 1
                """,
                cancellationToken: ct));

        return domains.AsList();
    }
}
