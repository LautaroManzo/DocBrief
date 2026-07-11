using DocBrief.Application.Interfaces;
using DocBrief.Infrastructure.AI;
using DocBrief.Infrastructure.Parsers;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "DocBrief API",
        Version = "v1",
        Description = "API REST que resume PDFs, Word y texto plano usando IA."
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
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Semantic Kernel + Ollama (desarrollo local) / Gemini (produccion)
builder.Services.AddKernel();

var ollamaHttpClient = new HttpClient
{
    BaseAddress = new Uri("http://localhost:11434"),
    Timeout = TimeSpan.FromMinutes(5)
};

#pragma warning disable SKEXP0070
builder.Services.AddOllamaChatCompletion(modelId: "llama3.2", httpClient: ollamaHttpClient);
#pragma warning restore SKEXP0070

// Servicios de aplicacion
builder.Services.AddScoped<ISummaryService, SemanticKernelSummaryService>();
builder.Services.AddScoped<IDocumentParser, PdfParser>();
builder.Services.AddScoped<IDocumentParser, DocxParser>();
builder.Services.AddScoped<IDocumentParserResolver, DocumentParserResolver>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(DocBrief.Application.Interfaces.ISummaryService).Assembly));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
