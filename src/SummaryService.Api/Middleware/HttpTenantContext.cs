using SummaryService.Application.Interfaces;
using System.Security.Claims;

namespace SummaryService.Api.Middleware;

public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    : ITenantContext
{
    public string? TenantId =>
        httpContextAccessor.HttpContext?.User
            .FindFirst("tenant_id")?.Value;

    public string? Role =>
        httpContextAccessor.HttpContext?.User
            .FindFirst(ClaimTypes.Role)?.Value;
}