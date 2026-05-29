using SummaryService.Application.Services;

namespace SummaryService.UnitTests.Services;

public class TokenEstimatorTests
{
    [Fact]
    public void Estimate_NullText_ReturnsZero()
    {
        var result = TokenEstimator.Estimate(null);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Estimate_EmptyText_ReturnsZero()
    {
        var result = TokenEstimator.Estimate("");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Estimate_WhitespaceText_ReturnsOne()
    {
        var result = TokenEstimator.Estimate("   ");

        Assert.Equal(1, result);
    }

    [Fact]
    public void Estimate_ShortText_EstimatesTokenCount()
    {
        var result = TokenEstimator.Estimate("Hello world");

        Assert.Equal(3, result);
    }

    [Fact]
    public void Estimate_LongText_EstimatesTokensCorrectly()
    {
        var text = new string('a', 100);

        var result = TokenEstimator.Estimate(text);

        Assert.Equal(26, result);
    }
}
