using DocBrief.Application.UseCases.SummarizeDocument;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DocBrief.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SummaryController : ControllerBase
{
    private readonly IMediator _mediator;

    public SummaryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("pdf")]
    public async Task<IActionResult> SummarizePdf(IFormFile file)
    {
        if (file.Length == 0 || file.ContentType != "application/pdf")
            return BadRequest("Se requiere un archivo PDF valido.");

        var command = new SummarizeDocumentCommand(file, null, "pdf");
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    [HttpPost("text")]
    public async Task<IActionResult> SummarizeText([FromBody] TextRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
            return BadRequest("Se requiere texto para resumir.");

        var command = new SummarizeDocumentCommand(null, request.Text, "text");
        var result = await _mediator.Send(command);

        return Ok(result);
    }
}

public record TextRequest(string Text);
