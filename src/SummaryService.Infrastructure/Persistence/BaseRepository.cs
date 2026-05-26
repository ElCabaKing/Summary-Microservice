using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using SummaryService.Domain.Options;

namespace SummaryService.Infrastructure.Persistence;

public abstract class BaseRepository
{
    private readonly string _connectionString;

    protected BaseRepository(IOptions<ConnectionStringsOptions> connectionStrings)
    {
        _connectionString = connectionStrings.Value.Default;
    }

    protected SqlConnection GetConnection() => new(_connectionString);
}
