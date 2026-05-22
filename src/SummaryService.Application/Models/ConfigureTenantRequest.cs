namespace SummaryService.Application.Models;

public sealed record ConfigureTenantRequest(
    string Provider,
    string ApiKey);
