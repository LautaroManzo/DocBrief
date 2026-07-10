using DocBrief.Application.Interfaces;
using Microsoft.SemanticKernel;

namespace DocBrief.Infrastructure.AI;

public class SemanticKernelSummaryService : ISummaryService
{
    private readonly Kernel _kernel;

    public SemanticKernelSummaryService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> SummarizeAsync(string text)
    {
        var prompt = @"Sos un asistente que resume documentos de forma clara y concisa.
Generá un resumen en español del siguiente texto.
El resumen debe capturar los puntos principales y ser fácil de entender.

Texto:
{{$input}}

Resumen:";

        var function = _kernel.CreateFunctionFromPrompt(prompt);
        var result = await _kernel.InvokeAsync(function, new() { ["input"] = text });

        return result.GetValue<string>() ?? string.Empty;
    }
}
