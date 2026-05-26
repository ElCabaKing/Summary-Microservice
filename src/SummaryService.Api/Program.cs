using Serilog;
using SummaryService.Api.Authentication;
using SummaryService.Api.Endpoints;
using SummaryService.Api.Middleware;
using SummaryService.Application.Interfaces;
using SummaryService.Application.UseCases;
using SummaryService.Domain.Options;
using SummaryService.Infrastructure.Chunking;
using SummaryService.Infrastructure.Encryption;
using SummaryService.Infrastructure.Factory;
using SummaryService.Infrastructure.Llm;
using SummaryService.Infrastructure.Llm.Options;
using SummaryService.Infrastructure.Pdf;
using SummaryService.Infrastructure.Persistence;
using SummaryService.Infrastructure.Sse;

DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

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

var sqlConnectionString = Environment.GetEnvironmentVariable("SQL_CONNECTION_STRING");
if (!string.IsNullOrEmpty(sqlConnectionString))
{
    builder.Configuration["ConnectionStrings:Default"] = sqlConnectionString;
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

//
// API Key Authentication
//
builder.Services
    .AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
    .AddScheme<ApiKeyAuthOptions, ApiKeyAuthHandler>(
        ApiKeyDefaults.AuthenticationScheme, null);

builder.Services.AddAuthorization();

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

builder.Services.Configure<ConnectionStringsOptions>(
    builder.Configuration.GetSection(ConnectionStringsOptions.SectionName));

//
// Multi-tenant
//
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<IApiKeyHashService, ApiKeyHashService>();

builder.Services.AddScoped<IStreamingTextGenerator,
    SemanticKernelStreamingTextGenerator>();

//
// SSE
//
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ISseStreamWriter, SseStreamWriter>();

//
// Use Cases
//
builder.Services.AddScoped<SummarizeDocumentUseCase>();
builder.Services.AddScoped<RegisterClientUseCase>();
builder.Services.AddScoped<RegenerateApiKeyUseCase>();

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

app.UseAuthentication();
app.UseAuthorization();

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
app.MapApiKeyEndpoints();

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
