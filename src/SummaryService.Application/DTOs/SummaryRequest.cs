namespace SummaryService.Application.DTOs;

public sealed record SummaryRequest(Stream Document, string FileName, string ContentType);
