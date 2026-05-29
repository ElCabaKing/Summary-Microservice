using System.Security.Cryptography;
using System.Text;
using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Encryption;

public sealed class ApiKeyHashService : IApiKeyHashService
{
    private const int KeyByteLength = 32;

    public string GenerateApiKey()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(KeyByteLength);
        return $"smm_{Convert.ToHexStringLower(randomBytes)}";
    }

    public string ComputeHash(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexStringLower(bytes);
    }

    public bool VerifyKey(string rawKey, string storedHash) =>
        string.Equals(ComputeHash(rawKey), storedHash, StringComparison.OrdinalIgnoreCase);
}
