namespace SummaryService.Application.Interfaces;

public interface ITenantContext
{
    string? TenantId { get; }
    string? Role { get; }
}
