using SummaryService.Application.Interfaces;

namespace SummaryService.Api.Middleware;

public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public string? TenantId =>
        httpContextAccessor.HttpContext?.User
            .FindFirst("tenant_id")?.Value;

    public string? Role =>
        httpContextAccessor.HttpContext?.User
            .FindFirst("role")?.Value;
}
