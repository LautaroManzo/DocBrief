using DocBrief.Application.UseCases.FixConceptMap;
using DocBrief.Application.UseCases.SummarizeDocument;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DocBrief.API.Controllers;

/// <summary>
/// Genera resúmenes de documentos (PDF, Word), texto plano, paginas web o videos
/// de YouTube (via su transcripcion) usando IA.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SummaryController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
    private const int MaxTextLength = 10_000;
    private static readonly string[] SupportedExtensions = { ".pdf", ".docx" };

    private readonly IMediator _mediator;

    public SummaryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Resume un archivo PDF o Word.
    /// </summary>
    /// <param name="file">Archivo a resumir (PDF o DOCX, maximo 10 MB).</param>
    /// <param name="summaryMode">Modo de resumen: "basico" o "estudio".</param>
    /// <param name="outputLanguage">Idioma del resumen: "es", "en" o "pt".</param>
    /// <param name="includeConceptMap">Si se incluye un mapa conceptual (solo aplica en modo "estudio").</param>
    /// <response code="200">Resumen generado correctamente.</response>
    /// <response code="400">El archivo es invalido, no tiene un formato soportado o supera los 10 MB.</response>
    [HttpPost("file")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(typeof(SummarizeDocumentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SummarizeFile(
        IFormFile file,
        [FromForm] string summaryMode = "basico",
        [FromForm] string outputLanguage = "es",
        [FromForm] bool includeConceptMap = false)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (file.Length == 0 || !SupportedExtensions.Contains(extension))
            return BadRequest("Se requiere un archivo PDF o Word valido.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("El archivo supera el limite de 10 MB.");

        try
        {
            var command = new SummarizeDocumentCommand(file, null, null, "file", summaryMode, outputLanguage, includeConceptMap);
            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Resume un texto plano.
    /// </summary>
    /// <param name="request">Texto a resumir junto con las opciones de modo e idioma.</param>
    /// <response code="200">Resumen generado correctamente.</response>
    /// <response code="400">No se envio texto, o supera los 10.000 caracteres.</response>
    [HttpPost("text")]
    [ProducesResponseType(typeof(SummarizeDocumentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SummarizeText([FromBody] TextRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Se requiere texto para resumir.");

        if (request.Text.Length > MaxTextLength)
            return BadRequest($"El texto supera el limite de {MaxTextLength} caracteres.");

        var command = new SummarizeDocumentCommand(
            null,
            request.Text,
            null,
            "text",
            request.SummaryMode ?? "basico",
            request.OutputLanguage ?? "es",
            request.IncludeConceptMap ?? false);

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Resume el contenido de una pagina web o un video de YouTube a partir de su URL.
    /// Si la URL es de YouTube, se usa la transcripcion del video en vez del HTML.
    /// </summary>
    /// <param name="request">URL a resumir junto con las opciones de modo e idioma.</param>
    /// <response code="200">Resumen generado correctamente.</response>
    /// <response code="400">
    /// La URL no es valida, no esta permitida, no se pudo acceder a ella, o el video
    /// de YouTube no tiene subtitulos disponibles.
    /// </response>
    [HttpPost("url")]
    [ProducesResponseType(typeof(SummarizeDocumentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SummarizeUrl([FromBody] UrlRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return BadRequest("Se requiere una URL para resumir.");

        try
        {
            var command = new SummarizeDocumentCommand(
                null,
                null,
                request.Url,
                "url",
                request.SummaryMode ?? "basico",
                request.OutputLanguage ?? "es",
                request.IncludeConceptMap ?? false);

            var result = await _mediator.Send(command);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (HttpRequestException)
        {
            return BadRequest("No se pudo acceder a esa URL.");
        }
    }

    /// <summary>
    /// Reintenta corregir la sintaxis de un mapa conceptual (mermaid) que fallo al
    /// renderizar en el navegador, preservando el contenido del diagrama.
    /// </summary>
    /// <param name="request">Codigo mermaid que fallo y el error devuelto por mermaid.</param>
    /// <response code="200">Diagrama corregido.</response>
    /// <response code="400">Falta el codigo o el error.</response>
    [HttpPost("fix-concept-map")]
    [ProducesResponseType(typeof(FixConceptMapResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FixConceptMap([FromBody] FixConceptMapRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Error))
            return BadRequest("Se requiere el codigo y el error del diagrama.");

        var result = await _mediator.Send(new FixConceptMapCommand(request.Code, request.Error));

        return Ok(result);
    }
}

/// <summary>
/// Datos para resumir un texto plano.
/// </summary>
/// <param name="Text">Texto a resumir.</param>
/// <param name="SummaryMode">Modo de resumen: "basico" o "estudio". Por defecto "basico".</param>
/// <param name="OutputLanguage">Idioma del resumen: "es", "en" o "pt". Por defecto "es".</param>
/// <param name="IncludeConceptMap">Si se incluye un mapa conceptual (solo aplica en modo "estudio").</param>
public record TextRequest(string Text, string? SummaryMode, string? OutputLanguage, bool? IncludeConceptMap = null);

/// <summary>
/// Datos para resumir una pagina web.
/// </summary>
/// <param name="Url">URL de la pagina a resumir.</param>
/// <param name="SummaryMode">Modo de resumen: "basico" o "estudio". Por defecto "basico".</param>
/// <param name="OutputLanguage">Idioma del resumen: "es", "en" o "pt". Por defecto "es".</param>
/// <param name="IncludeConceptMap">Si se incluye un mapa conceptual (solo aplica en modo "estudio").</param>
public record UrlRequest(string Url, string? SummaryMode, string? OutputLanguage, bool? IncludeConceptMap = null);

/// <summary>
/// Datos para reintentar un mapa conceptual que fallo al renderizar.
/// </summary>
/// <param name="Code">Codigo mermaid que fallo.</param>
/// <param name="Error">Mensaje de error devuelto por mermaid al intentar renderizarlo.</param>
public record FixConceptMapRequest(string Code, string Error);
