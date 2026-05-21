using FluentValidation;
using Serilog;
using SummaryService.Api.Endpoints;
using SummaryService.Api.Middleware;
using SummaryService.Application.Interfaces;
using SummaryService.Application.UseCases;
using SummaryService.Application.Validators;
using SummaryService.Infrastructure.Chunking;
using SummaryService.Infrastructure.Factory;
using SummaryService.Infrastructure.Llm;
using SummaryService.Domain.Options;
using SummaryService.Infrastructure.Llm.Options;
using SummaryService.Infrastructure.Pdf;
using SummaryService.Infrastructure.Sse;

// Load .env file
var envFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
if (File.Exists(envFile))
{
    foreach (var line in File.ReadAllLines(envFile))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
            continue;

        var parts = line.Split('=', StringSplitOptions.None);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var value = parts[1].Trim();
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Ensure environment variables are loaded into configuration
builder.Configuration.AddEnvironmentVariables();

// Map environment variables to configuration
var groqApiKey = Environment.GetEnvironmentVariable("GROQ_API_KEY");
if (!string.IsNullOrEmpty(groqApiKey))
{
    builder.Configuration["AI:Providers:groq:ApiKey"] = groqApiKey;
}

var geminiApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
if (!string.IsNullOrEmpty(geminiApiKey))
{
    builder.Configuration["AI:Providers:gemini:ApiKey"] = geminiApiKey;
}
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

//
// Validators
//
builder.Services.AddValidatorsFromAssemblyContaining<SummaryRequestValidator>();

//
// PDF
//
builder.Services.AddScoped<IDocumentParser, DocumentParser>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<IPdfOcrExtractor, PdfOcrExtractor>();

//
// Chunking
//
builder.Services.AddScoped<ITextChunker, TextChunker>();
builder.Services.AddScoped<ITokenEstimator, TokenEstimator>();

//
// Prompting
//
builder.Services.AddScoped<IPromptProvider, PromptProvider>();

//
// AI / LLM
//
builder.Services.Configure<AiOptions>(
    builder.Configuration.GetSection("AI"));

builder.Services.AddSingleton<IKernelFactory, KernelFactory>();

//
// Configuration Options
//
builder.Services.Configure<ChunkingOptions>(
    builder.Configuration.GetSection(ChunkingOptions.SectionName));

builder.Services.Configure<SummaryOptions>(
    builder.Configuration.GetSection(SummaryOptions.SectionName));

builder.Services.Configure<OcrOptions>(
    builder.Configuration.GetSection(OcrOptions.SectionName));

builder.Services.AddScoped<IStreamingTextGenerator,
    SemanticKernelStreamingTextGenerator>();

builder.Services.AddScoped<ISummaryGenerator, SummaryGenerator>();

//
// SSE
//
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ISseStreamWriter, SseStreamWriter>();

//
// Use Cases
//
builder.Services.AddScoped<SummarizeDocumentUseCase>();

//
// Swagger
//
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

//
// CORS
//
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

//
// Middleware
//
app.UseMiddleware<ExceptionMiddleware>();

app.UseSerilogRequestLogging();

app.UseCors();

//
// Swagger
//
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}

//
// Endpoints
//
app.MapSummaryEndpoints();

try
{
    Log.Information("Starting SummaryService");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;