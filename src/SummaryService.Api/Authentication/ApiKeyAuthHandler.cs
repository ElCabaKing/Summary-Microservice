using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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

        using var scope = scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();

        var keyHash = ComputeHash(apiKey);
        var keyEntity = await repo.GetByHashAsync(keyHash, Context.RequestAborted);

        if (keyEntity is null || !keyEntity.IsActive)
            return AuthenticateResult.Fail("API key inválida o inactiva");

        var claims = new[]
        {
            new Claim("tenant_id", keyEntity.TenantId),
            new Claim(ClaimTypes.Role, keyEntity.Role),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private static string ComputeHash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(bytes);
    }
}

public sealed class ApiKeyAuthOptions : AuthenticationSchemeOptions;

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}
