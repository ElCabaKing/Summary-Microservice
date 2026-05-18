namespace SummaryService.Domain.Constants;

public static class AppConstants
{
    public const int MaxFileSizeMb = 15;
    public const int MaxTokens = 2048;
    public const int MaxChunkTokens = 6000;
    public const int OverlapTokens = 300;
    public const int MaxChunks = 100;
    public const int OcrMaxPages = 100;
    public const int OcrDpi = 300;
    public const int OcrTimeoutSeconds = 60;

    public static readonly string[] AllowedMimeTypes = [
        "application/pdf",
        "text/plain"
    ];

    public const string DefaultModel = "llama-3.3-70b-versatile";
}
