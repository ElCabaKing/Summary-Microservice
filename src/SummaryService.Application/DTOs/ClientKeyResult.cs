namespace SummaryService.Application.DTOs;

public sealed class ClientKeyResult
{
    public string ApiKey { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
}
