
using SummaryService.Application.Models;
using SummaryService.Application.UseCases;

namespace SummaryService.Api.Endpoints;
public static class SummaryEndpoints
{
    public static void MapSummaryEndpoints(this WebApplication app)
    {
        app.MapGet("/health", () =>
            Results.Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow
            }))
            .WithName("HealthCheck");

        app.MapPost("/api/v1/summaries/stream", async (
            HttpContext context,
            SummarizeDocumentUseCase useCase,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            if (!context.Request.HasFormContentType)
            {
                await WriteSseError(
                    context,
                    "Expected multipart form data",
                    ct);

                return;
            }

            var form = await context.Request.ReadFormAsync(ct);

            var file = form.Files.GetFile("document");

            if (file is null || file.Length == 0)
            {
                await WriteSseError(
                    context,
                    "No document provided",
                    ct);

                return;
            }

            //
            // NEW
            //
            var provider = form["provider"].ToString();

            var model = form["model"].ToString();

            if (string.IsNullOrWhiteSpace(provider))
            {
                provider = "groq";
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                model = "llama-3.3-70b-versatile";
            }

            // Validate that requested model exists in configured provider models
            var modelsSection = configuration.GetSection($"AI:Providers:{provider}:Models");
            var allowedModels = modelsSection.Exists()
                ? modelsSection.Get<List<string>>() ?? new List<string>()
                : new List<string>();

            if (!allowedModels.Contains(model))
            {
                var allowed = allowedModels.Any() ? string.Join(", ", allowedModels) : "(no models configured)";
                await WriteSseError(
                    context,
                    $"Modelo '{model}' no permitido para provider '{provider}'. Modelos válidos: {allowed}",
                    ct);

                return;
            }

            await using var stream = file.OpenReadStream();

            var contentType = file.ContentType;

            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = Path.GetExtension(file.FileName)
                    .ToLowerInvariant() switch
                {
                    ".pdf" => "application/pdf",
                    ".txt" => "text/plain",
                    _ => "application/octet-stream"
                };
            }

            await useCase.ExecuteAsync(
                new SummaryStreamRequest
                {
                    Document = stream,
                    ContentType = contentType,
                    Provider = provider,
                    Model = model
                },
                ct);
        })
        .WithName("SummarizeStream")
        .DisableAntiforgery();
    }

    private static async Task WriteSseError(
        HttpContext context,
        string message,
        CancellationToken ct)
    {
        await context.Response.WriteAsync(
            $"event: error\ndata: {{\"error\":\"{message}\"}}\n\n",
            ct);
    }
}