using System.Security.Cryptography;
using System.Text;
using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Encryption;

public sealed class ApiKeyHashService : IApiKeyHashService
{
    private const string KeyPrefixConst = "smm_";
    private const int PrefixCharsLength = 10;
    private const int TotalPrefixLength = 14;
    private const int RandomBytesLength = 32;

    private static readonly char[] AllowedChars =
        "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    public string GenerateApiKey(out string prefix)
    {
        var prefixChars = new char[PrefixCharsLength];
        for (var i = 0; i < PrefixCharsLength; i++)
        {
            prefixChars[i] = AllowedChars[RandomNumberGenerator.GetInt32(AllowedChars.Length)];
        }

        var randomBytes = RandomNumberGenerator.GetBytes(RandomBytesLength);
        var key = $"{KeyPrefixConst}{new string(prefixChars)}{Convert.ToHexStringLower(randomBytes)}";
        prefix = key[..TotalPrefixLength];
        return key;
    }

    public string ComputeHash(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexStringLower(bytes);
    }

    public string GetPrefix(string rawKey) => rawKey[..TotalPrefixLength];

    public bool VerifyKey(string rawKey, string storedHash) =>
        string.Equals(ComputeHash(rawKey), storedHash, StringComparison.OrdinalIgnoreCase);
}
