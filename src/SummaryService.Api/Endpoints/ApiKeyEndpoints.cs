using SummaryService.Application.Interfaces;
using SummaryService.Application.UseCases;

namespace SummaryService.Api.Endpoints;

public static class ApiKeyEndpoints
{
    public static void MapApiKeyEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/admin/apikeys", async (
            RegisterClientRequest body,
            RegisterClientUseCase useCase,
            ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            if (tenantContext.Role != "admin")
                return Results.Forbid();

            var result = await useCase.ExecuteAsync(
                body.CompanyName,
                body.Email,
                body.ContactName,
                body.Domain,
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(result.Error.Message, statusCode: 400);
        })
        .WithName("RegisterClient")
        .DisableAntiforgery()
        .RequireAuthorization();

        app.MapPut("/api/v1/admin/apikeys/regenerate", async (
            RegenerateKeyRequest body,
            RegenerateApiKeyUseCase useCase,
            ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            if (tenantContext.Role != "admin")
                return Results.Forbid();

            var result = await useCase.ExecuteAsync(
                body.TenantId,
                ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(result.Error.Message, statusCode: 400);
        })
        .WithName("RegenerateApiKey")
        .DisableAntiforgery()
        .RequireAuthorization();
    }
}

public sealed record RegisterClientRequest(
    string CompanyName,
    string? Email,
    string? ContactName,
    string Domain);

public sealed record RegenerateKeyRequest(string TenantId);
