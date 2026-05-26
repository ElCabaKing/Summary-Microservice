using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SummaryService.Application.Interfaces;
using SummaryService.Domain.Entities;

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

        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();
        var hashService = scope.ServiceProvider.GetRequiredService<IApiKeyHashService>();

        if (apiKey.Length < 14)
            return AuthenticateResult.Fail("API key inválida");

        var prefix = hashService.GetPrefix(apiKey);
        var candidates = await repo.GetByPrefixAsync(prefix, Context.RequestAborted);

        ApiKey? match = null;
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


