using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SummaryService.Application.Interfaces;

namespace SummaryService.Infrastructure.Sse;

public sealed class SseStreamWriter(IHttpContextAccessor httpContextAccessor) : ISseStreamWriter
{
    private HttpResponse Response => httpContextAccessor.HttpContext?.Response
        ?? throw new InvalidOperationException("No HttpContext available");

    public async Task WriteStatusAsync(string status, CancellationToken ct)
    {
        await WriteEventAsync("status", JsonSerializer.Serialize(new { status }), ct);
    }

    public async Task WriteChunkAsync(string chunk, CancellationToken ct)
    {
        await WriteEventAsync("chunk", JsonSerializer.Serialize(new { data = chunk }), ct);
    }

    public async Task WriteCompletedAsync(string summary, CancellationToken ct)
    {
        await WriteEventAsync("completed", JsonSerializer.Serialize(new { summary }), ct);
    }

    public async Task WriteErrorAsync(string error, CancellationToken ct)
    {
        await WriteEventAsync("error", JsonSerializer.Serialize(new { error }), ct);
    }

    private async Task WriteEventAsync(string type, string data, CancellationToken ct)
    {
        await Response.WriteAsync($"event: {type}\n", ct);
        await Response.WriteAsync($"data: {data}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
