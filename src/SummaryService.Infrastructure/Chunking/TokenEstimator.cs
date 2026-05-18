using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Chunking;

public sealed class TokenEstimator : ITokenEstimator
{
    // Simple estimation: ~4 chars per token
    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        return text.Length / 4 + 1;
    }
}
