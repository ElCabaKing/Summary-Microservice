using SummaryService.Domain.Enums;

namespace SummaryService.UnitTests.Domain;

public class SummaryStatusTests
{
    [Fact]
    public void SummaryStatus_ShouldHaveExpectedValues()
    {
        Assert.Equal(0, (int)SummaryStatus.ExtractingText);
        Assert.Equal(1, (int)SummaryStatus.RunningOcr);
        Assert.Equal(2, (int)SummaryStatus.ChunkingDocument);
        Assert.Equal(3, (int)SummaryStatus.GeneratingSummary);
        Assert.Equal(4, (int)SummaryStatus.ReducingSummary);
        Assert.Equal(5, (int)SummaryStatus.Completed);
        Assert.Equal(6, (int)SummaryStatus.Failed);
    }
}
