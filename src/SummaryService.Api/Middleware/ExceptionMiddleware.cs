using System.Text.Json;
using SummaryService.Shared.Models;

namespace SummaryService.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Request was cancelled");
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var error = new Error("INTERNAL_ERROR", "An internal error occurred");
            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
