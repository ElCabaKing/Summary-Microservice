using SummaryService.Application.Validators;
using SummaryService.Application.DTOs;

namespace SummaryService.UnitTests.Application;

public class SummaryRequestValidatorTests
{
    private readonly SummaryRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidPdf_ShouldPass()
    {
        var request = new SummaryRequest(new MemoryStream(), "test.pdf", "application/pdf");
        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidContentType_ShouldFail()
    {
        var request = new SummaryRequest(new MemoryStream(), "test.exe", "application/x-msdownload");
        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
    }
}
