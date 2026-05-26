namespace SummaryService.Domain.Entities;

public sealed class Client
{
    public Guid Id { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? ContactName { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
