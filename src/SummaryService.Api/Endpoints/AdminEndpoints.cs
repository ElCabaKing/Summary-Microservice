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
    }
}
