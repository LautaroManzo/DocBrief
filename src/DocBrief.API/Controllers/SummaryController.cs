using DocBrief.Application.Interfaces;
using DocBrief.Application.UseCases.SummarizeDocument;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DocBrief.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SummaryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISummaryRepository _summaryRepository;

    public SummaryController(IMediator mediator, ISummaryRepository summaryRepository)
    {
        _mediator = mediator;
        _summaryRepository = summaryRepository;
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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var summary = await _summaryRepository.GetByIdAsync(id);
        if (summary is null)
            return NotFound();

        return Ok(new { summary.Id, summary.Content, summary.CreatedAt });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var summaries = await _summaryRepository.GetAllAsync();
        return Ok(summaries.Select(s => new { s.Id, s.Content, s.CreatedAt }));
    }
}

public record TextRequest(string Text);
