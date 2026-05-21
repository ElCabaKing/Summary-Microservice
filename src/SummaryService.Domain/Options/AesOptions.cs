namespace SummaryService.Domain.Options;

public sealed class AesOptions
{
    public const string SectionName = "Aes";

    public string Key { get; set; } = string.Empty;
}
