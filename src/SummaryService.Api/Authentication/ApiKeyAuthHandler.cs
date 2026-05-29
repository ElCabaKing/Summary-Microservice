using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SummaryService.Application.Interfaces;

namespace SummaryService.Api.Authentication;

public sealed class ApiKeyAuthHandler(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<ApiKeyAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<ApiKeyAuthOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var authHeaderValue = authHeader.ToString();
        if (!authHeaderValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var apiKey = authHeaderValue["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
            return AuthenticateResult.NoResult();

        if (!Request.Headers.TryGetValue("Origin", out var originHeader))
            return AuthenticateResult.Fail("Origin header requerido");

        var origin = originHeader.ToString();
        if (string.IsNullOrWhiteSpace(origin))
            return AuthenticateResult.Fail("Origin header inválido");

        using var scope = scopeFactory.CreateScope();
        var clientRepo = scope.ServiceProvider.GetRequiredService<IClientRepository>();
        var apiKeyRepo = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
        var hashService = scope.ServiceProvider.GetRequiredService<IApiKeyHashService>();

        var client = await clientRepo.GetByDomainAsync(origin, Context.RequestAborted);

        if (client is null)
            return AuthenticateResult.Fail("Cliente no encontrado para el dominio");

        var candidates = await apiKeyRepo.GetByTenantIdAsync(client.TenantId, Context.RequestAborted);

        Domain.Entities.ApiKey? match = null;
        foreach (var candidate in candidates)
        {
            if (candidate.IsActive && hashService.VerifyKey(apiKey, candidate.KeyHash))
            {
                match = candidate;
                break;
            }
        }

        if (match is null)
            return AuthenticateResult.Fail("API key inválida o inactiva");

        var claims = new[]
        {
            new Claim("tenant_id", match.TenantId),
            new Claim(ClaimTypes.Role, match.Role),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }
}

public sealed class ApiKeyAuthOptions : AuthenticationSchemeOptions;

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}


