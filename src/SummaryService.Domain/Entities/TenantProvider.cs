namespace SummaryService.Domain.Entities;

public sealed class TenantProvider
{
    public Guid Id { get; init; }
    public string TenantId { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string EncryptedApiKey { get; init; } = string.Empty;
    public string? Endpoint { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
