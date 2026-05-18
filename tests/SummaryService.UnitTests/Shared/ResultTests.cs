using SummaryService.Shared.Models;

namespace SummaryService.UnitTests.Shared;

public class ResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        var error = new Error("TEST", "Test error");
        var result = Result<int>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal(error, result.Error);
    }
}
