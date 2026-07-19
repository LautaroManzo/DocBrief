using DocBrief.Application.Interfaces;
using Microsoft.SemanticKernel;

namespace DocBrief.Infrastructure.AI;

public class SemanticKernelSummaryService : ISummaryService
{
    private readonly Kernel _kernel;

    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        ["es"] = "español",
        ["en"] = "ingles"
    };

    public SemanticKernelSummaryService(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<string> SummarizeAsync(string text, string summaryMode, string outputLanguage, bool includeConceptMap = false)
    {
        var languageName = LanguageNames.GetValueOrDefault(outputLanguage, LanguageNames["es"]);

        var prompt = summaryMode == "estudio"
            ? BuildStudyPrompt(languageName, includeConceptMap)
            : BuildBasicPrompt(languageName);

        var function = _kernel.CreateFunctionFromPrompt(prompt);
        var result = await _kernel.InvokeAsync(function, new() { ["input"] = text });

        return result.GetValue<string>() ?? string.Empty;
    }

    // Modo "Básico": sintesis clara y concisa del texto.
    private static string BuildBasicPrompt(string languageName)
    {
        return """
            Sos un asistente que resume documentos de forma clara y concisa.
            Genera un resumen en {LANGUAGE} del siguiente texto, en 1-2 parrafos, capturando
            los puntos principales sin entrar en demasiado detalle.
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
            .Replace("{LANGUAGE}", languageName);
    }

    // Modo "Plan de estudio": material de estudio completo aplicando tecnicas de aprendizaje
    // (chunking, elaboracion, glosario y active recall).
    private static string BuildStudyPrompt(string languageName, bool includeConceptMap)
    {
        var conceptMapInstructions = includeConceptMap
            ? """

                4. Una seccion "## Mapa conceptual" con un diagrama mermaid tipo mindmap
                   de los temas principales y como se relacionan. Formato exacto:
                   - Un bloque de codigo que empieza con ```mermaid y termina con ```.
                   - Adentro, primera linea "mindmap".
                   - Segunda linea con el nodo raiz: dos espacios de indentacion, luego
                     root((Tema principal)).
                   - Debajo, los subtemas indentados con 2 espacios mas por cada nivel
                     (maximo 3 niveles de profundidad).
                   - Los textos de los nodos deben ser cortos (2-5 palabras), sin parentesis,
                     comillas, dos puntos ni **negrita** adentro, porque rompen el diagrama.

                """
            : "\n";

        return """
            Sos un tutor que arma un plan de estudio a partir de un documento, para que el
            usuario pueda aprenderlo en profundidad. Escribi todo en {LANGUAGE}.

            Aplica estas tecnicas de estudio comprobadas:
            - Chunking: dividi el contenido en secciones tematicas claras y jerarquicas.
            - Elaboracion: no solo enumeres, explica cada concepto con contexto, el "por que"
              y ejemplos cuando ayuden a entender.
            - Glosario: destaca y defini los terminos clave para repaso rapido, nombrando el
              autor de ese termino cuando corresponda.
            - Active recall: cerra con preguntas de autoevaluacion y sus respuestas, la tecnica
              mas efectiva para fijar lo aprendido.

            Respondé unicamente con el material. No agregues introducciones, frases como
            "Aqui tenes" o "Claro que si", ni comentarios antes o despues.

            El resultado debe ser extenso y completo, cubriendo la mayor cantidad de detalles
            relevantes posible sin omitir nada importante. Estructuralo asi, en Markdown:

            1. Una sintesis inicial de 2-3 oraciones con la idea general (sin titulo).
            2. Desarrolla cada tema en su propia seccion con subtitulo "## ", explicando con
               contexto, detalle y ejemplos. Resalta los conceptos importantes con **negrita**
               y usa listas con "- " cuando ayude a la lectura.
            3. Una seccion "## Terminos clave" con los conceptos mas importantes y su
               definicion breve, en formato "- **Termino**: definicion".
            {CONCEPT_MAP}
            Por ultimo, una seccion "## Preguntas de repaso" con 4-5 preguntas para
               autoevaluarse, cada una seguida de su respuesta. Formato por pregunta: la
               pregunta en negrita "**1. ¿Pregunta?**", luego una linea en blanco, y despues
               la respuesta. Dejá una linea en blanco entre cada par de pregunta y respuesta.

            No uses tablas ni elementos de Markdown fuera de los mencionados (fuera del
            bloque mermaid del mapa conceptual, si se pidio).

            Texto:
            {{$input}}

            Plan de estudio:
            """
            .Replace("{LANGUAGE}", languageName)
            .Replace("{CONCEPT_MAP}", conceptMapInstructions);
    }
}
