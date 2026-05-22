using SummaryService.Application.Interfaces;
using SummaryService.Application.UseCases;

namespace SummaryService.Api.Endpoints;

public static class ApiKeyEndpoints
{
    public static void MapApiKeyEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/admin/apikeys", async (
            CreateApiKeyRequest body,
            CreateApiKeyUseCase useCase,
            ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            if (tenantContext.Role != "admin")
                return Results.Forbid();

            var result = await useCase.ExecuteAsync(
                body.TenantId,
                body.Role,
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(result.Error.Message, statusCode: 400);
        })
        .WithName("CreateApiKey")
        .DisableAntiforgery()
        .RequireAuthorization();
    }
}

public sealed record CreateApiKeyRequest(string TenantId, string Role);
