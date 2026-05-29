namespace SummaryService.Domain.Entities;

public sealed class ApiKey
{
    public Guid Id { get; init; }
    public string KeyHash { get; init; } = string.Empty;
    public string TenantId { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static ApiKey Create(string keyHash, string tenantId, string role = "user")
    {
        return new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = keyHash,
            TenantId = tenantId,
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}
