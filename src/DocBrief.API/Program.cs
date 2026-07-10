using DocBrief.Application.Interfaces;
using DocBrief.Infrastructure.AI;
using DocBrief.Infrastructure.Parsers;
using Microsoft.SemanticKernel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Semantic Kernel + Ollama (desarrollo local) / Gemini (produccion)
builder.Services.AddKernel();

#pragma warning disable SKEXP0070
builder.Services.AddOllamaChatCompletion("llama3.2", new Uri("http://localhost:11434"));
#pragma warning restore SKEXP0070

// Servicios de aplicacion
builder.Services.AddScoped<ISummaryService, SemanticKernelSummaryService>();
builder.Services.AddScoped<IDocumentParser, PdfParser>();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(DocBrief.Application.Interfaces.ISummaryService).Assembly));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
