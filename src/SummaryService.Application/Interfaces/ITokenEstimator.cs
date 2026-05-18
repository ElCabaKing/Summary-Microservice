namespace SummaryService.Application.Interfaces;

public interface ITokenEstimator
{
    int EstimateTokens(string text);
}
