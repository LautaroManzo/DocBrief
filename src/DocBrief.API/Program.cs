using System.Threading.RateLimiting;
using DocBrief.Application.Interfaces;
using DocBrief.Infrastructure.AI;
using DocBrief.Infrastructure.Parsers;
using DocBrief.Infrastructure.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "DocBrief API",
        Version = "v1",
        Description = "API REST que resume PDFs, Word, texto plano, paginas web y videos de YouTube (por URL) usando IA."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"]
                ?? throw new InvalidOperationException("Falta configurar Cors:AllowedOrigin en produccion.");

            policy.WithOrigins(allowedOrigin)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
    });
});

static string GetClientIp(HttpContext httpContext) =>
    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

var perMinuteLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: GetClientIp(httpContext),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));

var perDayLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: GetClientIp(httpContext),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromDays(1),
            QueueLimit = 0
        }));

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.CreateChained(perMinuteLimiter, perDayLimiter);

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Demasiadas solicitudes. Esperá un momento antes de intentar de nuevo.", cancellationToken);
    };
});

// Semantic Kernel + Ollama (desarrollo local) / Gemini (produccion)
builder.Services.AddKernel();

var aiProvider = builder.Configuration["AI:Provider"] ?? "Ollama";

if (aiProvider == "Gemini")
{
    var geminiApiKey = builder.Configuration["Gemini:ApiKey"]
        ?? throw new InvalidOperationException("Falta la API key de Gemini en la configuracion.");

#pragma warning disable SKEXP0070
    builder.Services.AddGoogleAIGeminiChatCompletion("gemini-flash-lite-latest", geminiApiKey);
#pragma warning restore SKEXP0070
}
else
{
    var ollamaHttpClient = new HttpClient
    {
        BaseAddress = new Uri("http://localhost:11434"),
        Timeout = TimeSpan.FromMinutes(5)
    };

#pragma warning disable SKEXP0070
    builder.Services.AddOllamaChatCompletion(modelId: "llama3.2", httpClient: ollamaHttpClient);
#pragma warning restore SKEXP0070
}

// Servicios de aplicacion
builder.Services.AddScoped<ISummaryService, SemanticKernelSummaryService>();
builder.Services.AddScoped<IDocumentParser, PdfParser>();
builder.Services.AddScoped<IDocumentParser, DocxParser>();
builder.Services.AddScoped<IDocumentParserResolver, DocumentParserResolver>();

builder.Services.AddHttpClient<IUrlContentFetcher, UrlContentFetcher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("DocBrief/1.0 (+https://github.com/LautaroManzo/DocBrief)");
})
.ConfigurePrimaryHttpMessageHandler(SsrfSafeHttpClientHandler.Create);

builder.Services.AddScoped<IYouTubeTranscriptFetcher, YouTubeTranscriptFetcher>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(DocBrief.Application.Interfaces.ISummaryService).Assembly));

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();
