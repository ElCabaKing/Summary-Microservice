using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Cors;

public sealed class DynamicCorsPolicyProvider(
    IServiceScopeFactory scopeFactory,
    ILogger<DynamicCorsPolicyProvider> logger) : ICorsPolicyProvider
{
    public async Task<CorsPolicy?> GetPolicyAsync(
        HttpContext context,
        string? policyName)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IClientRepository>();
            var domains = await repo.GetAllDomainsAsync(context.RequestAborted);

            var origins = new List<string>();

            var requestOrigin = context.Request.Headers.Origin.FirstOrDefault();
            if (!string.IsNullOrEmpty(requestOrigin))
                origins.Add(requestOrigin);

            origins.AddRange(domains);

            var builder = new CorsPolicyBuilder();
            if (origins.Count > 0)
                builder.WithOrigins(origins.ToArray());

            builder
                .AllowAnyHeader()
                .AllowAnyMethod();

            return builder.Build();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load CORS policy from database, falling back to permissive policy");
            return new CorsPolicyBuilder()
                .AllowAnyHeader()
                .AllowAnyMethod()
                .Build();
        }
    }
}
