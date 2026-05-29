namespace SummaryService.Application.Interfaces;

public interface IApiKeyHashService
{
    string GenerateApiKey();
    string ComputeHash(string rawKey);
    bool VerifyKey(string rawKey, string storedHash);
}
