namespace SummaryService.Domain.Options;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    public string Default { get; set; } = string.Empty;
}
