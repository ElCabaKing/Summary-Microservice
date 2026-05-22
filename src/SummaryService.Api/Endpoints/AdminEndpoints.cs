using SummaryService.Application.Interfaces;
using SummaryService.Application.Models;
using SummaryService.Application.UseCases;

namespace SummaryService.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/admin/tenants/{tenantId}/providers", async (
            string tenantId,
            ConfigureTenantRequest body,
            ConfigureTenantProviderUseCase useCase,
            ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            if (tenantContext.Role != "admin")
                return Results.Forbid();

            var result = await useCase.ExecuteAsync(tenantId, body, ct);

            return result.IsSuccess
                ? Results.Ok(new { message = "Provider configurado exitosamente" })
                : Results.Problem(result.Error.Message, statusCode: 400);
        })
        .WithName("ConfigureTenantProvider")
        .DisableAntiforgery()
        .RequireAuthorization();

        app.MapGet("/api/v1/admin/tenants/{tenantId}/providers", async (
            string tenantId,
            ITenantProviderRepository repo,
            ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            if (tenantContext.Role != "admin")
                return Results.Forbid();

            var providers = await repo.GetAllProvidersAsync(tenantId, ct);

            return Results.Ok(providers.Select(p => new
            {
                p.Provider,
                p.IsActive,
                p.CreatedAt,
                p.UpdatedAt
            }));
        })
        .WithName("ListTenantProviders")
        .DisableAntiforgery()
        .RequireAuthorization();

        app.MapDelete("/api/v1/admin/tenants/{tenantId}/providers/{provider}", async (
            string tenantId,
            string provider,
            ITenantProviderRepository repo,
            ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            if (tenantContext.Role != "admin")
                return Results.Forbid();

            await repo.DeleteProviderAsync(tenantId, provider, ct);

            return Results.Ok(new { message = $"Provider '{provider}' eliminado" });
        })
        .WithName("DeleteTenantProvider")
        .DisableAntiforgery()
        .RequireAuthorization();
    }
}
