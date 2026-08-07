using Clausio.Legal.API.Middleware;
using Clausio.Legal.Cache;
using Clausio.Legal.Core.Settings;
using Clausio.Legal.Infrastructure;
using Clausio.Legal.Infrastructure.Ai.Providers;
using Clausio.Legal.Infrastructure.Ai.Router;
using Clausio.Legal.Core.Interfaces.AI;
using Clausio.Legal.Infrastructure.Extraction;
using Clausio.Legal.Infrastructure.Storage;
using Clausio.Legal.Service;
using Clausio.Legal.Service.AI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Clausio.MCP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Controllers with JSON fix
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// JWT Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

// Database
builder.Services.AddDbContext<ClausioDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"), o => o.UseVector()));

// Cache
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// Storage
var storageRootPath = builder.Configuration["Storage:RootPath"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "documents");
builder.Services.AddSingleton<IDocumentStorage>(new LocalDiskDocumentStorage(storageRootPath));

// OCR & Document text extraction
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.OCR.IOCRProvider, Clausio.Legal.Infrastructure.OCR.PaddleOCRProvider>();
builder.Services.AddScoped<IDocumentTextExtractionStrategy, TxtExtractionStrategy>();
builder.Services.AddScoped<IDocumentTextExtractionStrategy, OcrExtractionStrategy>();
builder.Services.AddScoped<IDocumentTextExtractor, DocumentTextExtractor>();

// MCP Server Integration
builder.Services.AddClausioMcp();

// AI
builder.Services.AddHttpClient<Clausio.Legal.Infrastructure.Ai.Providers.TokenRouterProvider>();
builder.Services.AddHttpClient<Clausio.Legal.Infrastructure.Ai.Providers.OpenRouterProvider>();
builder.Services.AddHttpClient<Clausio.Legal.Infrastructure.Ai.Providers.OpenAIEmbeddingProvider>();

builder.Services.AddScoped<Clausio.Legal.Infrastructure.Ai.Providers.TokenRouterProvider>();
builder.Services.AddScoped<Clausio.Legal.Infrastructure.Ai.Providers.OpenRouterProvider>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Embedding.IEmbeddingProvider, Clausio.Legal.Infrastructure.Ai.Providers.OpenAIEmbeddingProvider>();
builder.Services.AddScoped<ILLMProvider>(sp => sp.GetRequiredService<Clausio.Legal.Infrastructure.Ai.Providers.TokenRouterProvider>());
builder.Services.AddScoped<IAIRouter, AIRouter>();
builder.Services.AddSingleton<AiResponseParser>();

builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Retrieval.IChunkProcessor, Clausio.Legal.Service.Retrieval.Chunking.ChunkProcessor>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Retrieval.IRetriever, Clausio.Legal.Infrastructure.Vector.PgVectorRetriever>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Retrieval.IChunkRanker, Clausio.Legal.Service.Retrieval.Ranking.ChunkRanker>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Retrieval.IBM25Retriever, Clausio.Legal.Service.Retrieval.Hybrid.BM25Retriever>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Retrieval.IHybridRetriever, Clausio.Legal.Service.Retrieval.Hybrid.HybridRetriever>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Retrieval.ICitationExtractor, Clausio.Legal.Service.Retrieval.Citation.CitationExtractor>();
// builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Retrieval.ICitationVerifier, Clausio.Legal.Service.Retrieval.Citation.CitationVerifier>(); // Replaced
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Retrieval.IRetrievalEngine, Clausio.Legal.Service.Retrieval.RetrievalEngine>();

builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Memory.IMemoryStore, Clausio.Legal.Service.Memory.MemoryStore>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Memory.IContextEngine, Clausio.Legal.Service.Memory.ContextEngine>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.Retrieval.IContextRanker, Clausio.Legal.Service.Retrieval.ContextRanking.ContextRanker>();

builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.OCR.IOCRProvider, Clausio.Legal.Infrastructure.OCR.PaddleOCRProvider>();
builder.Services.AddScoped<Clausio.Legal.Service.DocumentIntelligence.LayoutAnalyzer>();
builder.Services.AddScoped<Clausio.Legal.Service.DocumentIntelligence.ClauseDetector>();
builder.Services.AddScoped<Clausio.Legal.Service.DocumentIntelligence.TableExtractor>();
builder.Services.AddScoped<Clausio.Legal.Service.DocumentIntelligence.IDocumentProcessor, Clausio.Legal.Service.DocumentIntelligence.DocumentProcessor>();

builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.AI.IPromptBuilder, Clausio.Legal.Infrastructure.Ai.Prompts.PromptBuilder>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.AI.Validation.ICitationVerifier, Clausio.Legal.Service.AI.Validation.CitationVerifier>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.AI.Drafting.IDraftValidationPipeline, Clausio.Legal.Service.AI.Drafting.Validation.DraftValidationPipeline>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.AI.Drafting.IDraftEngine, Clausio.Legal.Service.AI.Drafting.DraftEngine>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.AI.Research.IDeepResearchPipeline, Clausio.Legal.Service.AI.Research.DeepResearchPipeline>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.AI.Evaluation.IAIEvaluator, Clausio.Legal.Service.AI.Evaluation.AIEvaluator>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.AI.Evaluation.ITelemetryService, Clausio.Legal.Service.AI.Evaluation.TelemetryService>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.AI.Security.IAISecurityLayer, Clausio.Legal.Service.AI.Security.AISecurityLayer>();
builder.Services.AddScoped<Clausio.Legal.Core.Interfaces.AI.Pipeline.IAIPipeline, Clausio.Legal.Service.AI.Pipeline.AIPipeline>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ICaseService, CaseService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<IActionPlanService, ActionPlanService>();
builder.Services.AddScoped<IContradictionService, ContradictionService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IHearingService, HearingService>();
builder.Services.AddScoped<ILegalResearchService, LegalResearchService>();
builder.Services.AddScoped<ITimelineService, TimelineService>();
builder.Services.AddScoped<IReadinessService, ReadinessService>();
builder.Services.AddScoped<IStatsService, StatsService>();
builder.Services.AddScoped<IAiService, AiService>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Secret"] ??
                    throw new InvalidOperationException("Jwt:Secret is not configured")
                )),
            NameClaimType = ClaimTypes.NameIdentifier,
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.SetIsOriginAllowed(origin => 
                    origin.StartsWith("http://localhost:") || 
                    origin.StartsWith("http://127.0.0.1:") || 
                    origin.EndsWith("clausio.app") || 
                    origin.EndsWith("clausio.io"))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

// CORS must be first middleware in pipeline
app.UseCors("AllowFrontend");

// Middleware
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Clausio Legal API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<DeviceBindingMiddleware>();
app.MapControllers();

app.Run();