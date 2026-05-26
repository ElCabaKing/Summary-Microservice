namespace SummaryService.Application.Interfaces;

public interface IApiKeyHashService
{
    string GenerateApiKey(out string prefix);
    string ComputeHash(string rawKey);
    string GetPrefix(string rawKey);
    bool VerifyKey(string rawKey, string storedHash);
}
