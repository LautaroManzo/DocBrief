using DocBrief.Application.Interfaces;
using Microsoft.SemanticKernel;

namespace DocBrief.Infrastructure.AI;

public class SemanticKernelSummaryService : ISummaryService
{
    private readonly Kernel _kernel;

    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        ["es"] = "español",
        ["en"] = "ingles",
        ["pt"] = "portugues"
    };

    // Compartido entre los dos modos: evita que el modelo "prolijice" nombres/terminos
    // mal transcriptos (comun en audio de video) inventando una grafia plausible.
    private const string FidelityInstructions = """
        Fidelidad con nombres y terminos tecnicos (clave si el texto viene de una
        transcripcion de audio/video, que puede tener errores):
        - Si mencionás un autor, teoria o termino tecnico especifico, verificá que sea
          reconocible en la bibliografia academica del campo. Si no estas seguro de la
          ortografia exacta de un nombre propio o termino, escribilo tal como aparece y
          marcalo con "[verificar: NOMBRE]" en vez de inventar una grafia que suene mas
          prolija.
        - No fusiones ni "corrijas" palabras que suenan parecido en el texto original
          para armar un termino gramaticalmente prolijo pero inexistente. Si algo no te
          resulta reconocible como termino establecido en la disciplina, transcribilo tal
          como esta y marcalo igual que un nombre incierto.
        - Distingui lo que la fuente afirma literalmente de lo que vos generalizas para
          dar cohesion — si generalizas, aclaralo con una frase como "en terminos
          generales" o "esto sugiere que", en vez de presentarlo como un hecho literal.
        """;

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
            Genera un resumen en {LANGUAGE} del siguiente texto, capturando los puntos
            principales sin entrar en demasiado detalle.
            Respondé unicamente con el resumen. No agregues introducciones, frases como
            "Aqui tenes" o "Claro que si", ni ningun comentario antes o despues del resumen.

            Reglas importantes:
            - Empeza siempre con un titulo breve (una linea, "# Titulo") que identifique
              el tema central del documento.
            - Especificidad: no mezcles conceptos distintos en una misma oracion ni los
              presentes como si fueran lo mismo. Cada idea debe quedar clara y diferenciada,
              aunque el resumen sea corto.
            - Claridad teorica antes que brevedad: si un concepto necesita su propia oracion
              para entenderse bien, dasela en vez de comprimirlo junto a otro para ahorrar
              espacio.
            - El resumen en si (despues del titulo) va en 1-2 parrafos.

            {FIDELITY}

            Formato (Markdown simple):
            - El titulo va con "# " (una sola vez, al principio).
            - Si el contenido tiene varios temas, organizalo con subtitulos usando "## ".
            - Usa "- " para listar puntos clave cuando ayude a la lectura.
            - Resalta los terminos o conceptos mas importantes usando **negrita**.
            - No uses tablas ni otros elementos de Markdown fuera de los mencionados.

            Texto:
            {{$input}}

            Resumen:
            """
            .Replace("{LANGUAGE}", languageName)
            .Replace("{FIDELITY}", FidelityInstructions);
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
                   - Usa exactamente los mismos terminos que aparecen en el cuerpo del texto,
                     no generes variantes ni sinonimos.

                """
            : "\n";

        return """
            Sos un tutor que arma un plan de estudio a partir de un documento, para que el
            usuario pueda aprenderlo en profundidad. Escribi todo en {LANGUAGE}.

            Aplica estas tecnicas de estudio comprobadas:
            - Chunking: dividi el contenido en secciones tematicas claras y jerarquicas, sin
              mezclar conceptos distintos dentro de una misma seccion.
            - Elaboracion: no solo enumeres, explica cada concepto con contexto, el "por que"
              y ejemplos cuando ayuden a entender. Priorizá la claridad teorica por sobre la
              brevedad: si dos conceptos son distintos, tratalos por separado en vez de
              fusionarlos para resumir mas rapido.
            - Glosario: destaca y defini los terminos clave para repaso rapido, nombrando el
              autor de ese termino cuando corresponda.
            - Active recall: cerra con preguntas de autoevaluacion y sus respuestas, la tecnica
              mas efectiva para fijar lo aprendido.

            {FIDELITY}
            - Si se mencionan ejemplos concretos, casos o aplicaciones practicas, incluilos —
              no te quedes solo con la teoria abstracta.

            Respondé unicamente con el material. No agregues introducciones, frases como
            "Aqui tenes" o "Claro que si", ni comentarios antes o despues.

            El resultado debe ser extenso y completo, cubriendo la mayor cantidad de detalles
            relevantes posible sin omitir nada importante. Estructuralo asi, en Markdown:

            1. Un titulo breve (una linea, "# Titulo") que identifique el documento, seguido
               de una sintesis de 2-3 oraciones con la idea general.
            2. Desarrolla cada tema en su propia seccion con subtitulo "## ", explicando con
               contexto, detalle y ejemplos. Mantene cada concepto separado y especifico — no
               los mezcles ni los generalices solo para resumir mas rapido. Resalta los
               conceptos importantes con **negrita** y usa listas con "- " cuando ayude a la
               lectura.
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
            .Replace("{FIDELITY}", FidelityInstructions)
            .Replace("{CONCEPT_MAP}", conceptMapInstructions);
    }
}
