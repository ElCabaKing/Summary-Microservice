using FluentValidation;
using Microsoft.SemanticKernel;
using Serilog;
using SummaryService.Api.Endpoints;
using SummaryService.Api.Middleware;
using SummaryService.Application.Interfaces;
using SummaryService.Application.UseCases;
using SummaryService.Application.Validators;
using SummaryService.Infrastructure.Chunking;
using SummaryService.Infrastructure.Llm;
using SummaryService.Infrastructure.Pdf;
using SummaryService.Infrastructure.Sse;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddValidatorsFromAssemblyContaining<SummaryRequestValidator>();

builder.Services.AddScoped<IDocumentParser, DocumentParser>();
builder.Services.AddScoped<IPdfTextExtractor, PdfTextExtractor>();
builder.Services.AddScoped<IPdfOcrExtractor, PdfOcrExtractor>();
builder.Services.AddScoped<ITextChunker, TextChunker>();
builder.Services.AddScoped<ITokenEstimator, TokenEstimator>();
builder.Services.AddScoped<IPromptProvider, PromptProvider>();
builder.Services.AddScoped<ISummaryGenerator, SummaryGenerator>();
builder.Services.AddScoped<IStreamingTextGenerator, GroqTextGenerator>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISseStreamWriter, SseStreamWriter>();
builder.Services.AddScoped<SummarizeDocumentUseCase>();

builder.Services.Configure<GroqOptions>(
    builder.Configuration.GetSection("Groq"));

builder.Services.AddKernel();
builder.Services.AddOpenAIChatCompletion(
    modelId: builder.Configuration["Groq:Model"] ?? "llama-3.3-70b-versatile",
    apiKey: builder.Configuration["Groq:ApiKey"] ?? string.Empty,
    endpoint: new Uri("https://api.groq.com/openai/v1"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

public partial class Program { }
