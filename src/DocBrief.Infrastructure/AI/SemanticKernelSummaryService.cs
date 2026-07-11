using DocBrief.Application.Interfaces;
using Microsoft.SemanticKernel;

namespace DocBrief.Infrastructure.AI;

public class SemanticKernelSummaryService : ISummaryService
{
    private readonly Kernel _kernel;

    private static readonly Dictionary<string, string> LengthInstructions = new()
    {
        ["corto"] = "en 2-3 oraciones, muy conciso",
        ["medio"] = "en un parrafo breve con los puntos principales",
        ["detallado"] = "de forma detallada, incluyendo contexto y matices relevantes"
    };

    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        ["es"] = "español",
        ["en"] = "ingles"
    };

    public SemanticKernelSummaryService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> SummarizeAsync(string text, string summaryLength, string outputLanguage)
    {
        var lengthInstruction = LengthInstructions.GetValueOrDefault(summaryLength, LengthInstructions["medio"]);
        var languageName = LanguageNames.GetValueOrDefault(outputLanguage, LanguageNames["es"]);

        var prompt = """
            Sos un asistente que resume documentos para que el usuario pueda estudiarlos facilmente.
            Genera un resumen en {LANGUAGE} del siguiente texto, {LENGTH}.
            Respondé unicamente con el resumen. No agregues introducciones, frases como
            "Aqui tenes" o "Claro que si", ni ningun comentario antes o despues del resumen.

            Formato (Markdown simple):
            - Si el contenido tiene varios temas, organizalo con subtitulos usando "## ".
            - Usa "- " para listar puntos clave cuando ayude a la lectura.
            - Resalta los terminos o conceptos mas importantes usando **negrita**.
            - No uses tablas ni otros elementos de Markdown fuera de los mencionados.

            Texto:
            {{$input}}

            Resumen:
            """
            .Replace("{LANGUAGE}", languageName)
            .Replace("{LENGTH}", lengthInstruction);

        var function = _kernel.CreateFunctionFromPrompt(prompt);
        var result = await _kernel.InvokeAsync(function, new() { ["input"] = text });

        return result.GetValue<string>() ?? string.Empty;
    }
}
