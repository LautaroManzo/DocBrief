using DocBrief.Application.UseCases.SummarizeDocument;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DocBrief.API.Controllers;

/// <summary>
/// Genera resúmenes de documentos (PDF, Word) o texto plano usando IA.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SummaryController : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;
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
    /// <param name="summaryLength">Largo del resumen: "corto", "medio" o "detallado".</param>
    /// <param name="outputLanguage">Idioma del resumen: "es" o "en".</param>
    /// <response code="200">Resumen generado correctamente.</response>
    /// <response code="400">El archivo es invalido, no tiene un formato soportado o supera los 10 MB.</response>
    [HttpPost("file")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType(typeof(SummarizeDocumentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SummarizeFile(
        IFormFile file,
        [FromForm] string summaryLength = "medio",
        [FromForm] string outputLanguage = "es")
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (file.Length == 0 || !SupportedExtensions.Contains(extension))
            return BadRequest("Se requiere un archivo PDF o Word valido.");

        if (file.Length > MaxFileSizeBytes)
            return BadRequest("El archivo supera el limite de 10 MB.");

        var command = new SummarizeDocumentCommand(file, null, "file", summaryLength, outputLanguage);
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Resume un texto plano.
    /// </summary>
    /// <param name="request">Texto a resumir junto con las opciones de largo e idioma.</param>
    /// <response code="200">Resumen generado correctamente.</response>
    /// <response code="400">No se envio texto para resumir.</response>
    [HttpPost("text")]
    [ProducesResponseType(typeof(SummarizeDocumentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SummarizeText([FromBody] TextRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Se requiere texto para resumir.");

        var command = new SummarizeDocumentCommand(
            null,
            request.Text,
            "text",
            request.SummaryLength ?? "medio",
            request.OutputLanguage ?? "es");

        var result = await _mediator.Send(command);

        return Ok(result);
    }
}

/// <summary>
/// Datos para resumir un texto plano.
/// </summary>
/// <param name="Text">Texto a resumir.</param>
/// <param name="SummaryLength">Largo del resumen: "corto", "medio" o "detallado". Por defecto "medio".</param>
/// <param name="OutputLanguage">Idioma del resumen: "es" o "en". Por defecto "es".</param>
public record TextRequest(string Text, string? SummaryLength, string? OutputLanguage);
