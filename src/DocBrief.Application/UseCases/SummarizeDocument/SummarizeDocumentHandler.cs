using DocBrief.Application.Interfaces;
using DocBrief.Domain.Entities;
using MediatR;

namespace DocBrief.Application.UseCases.SummarizeDocument;

public class SummarizeDocumentHandler : IRequestHandler<SummarizeDocumentCommand, SummarizeDocumentResult>
{
    private readonly IDocumentParser _pdfParser;
    private readonly ISummaryService _summaryService;
    private readonly ISummaryRepository _summaryRepository;

    public SummarizeDocumentHandler(
        IDocumentParser pdfParser,
        ISummaryService summaryService,
        ISummaryRepository summaryRepository)
    {
        _pdfParser = pdfParser;
        _summaryService = summaryService;
        _summaryRepository = summaryRepository;
    }

    public async Task<SummarizeDocumentResult> Handle(SummarizeDocumentCommand request, CancellationToken cancellationToken)
    {
        var extractedText = request.ContentType switch
        {
            "pdf" => await _pdfParser.ParseAsync(request.File!),
            "text" => request.Text!,
            _ => throw new ArgumentException($"Unsupported content type: {request.ContentType}")
        };

        var summaryContent = await _summaryService.SummarizeAsync(extractedText);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = request.File?.FileName ?? "text-input",
            ContentType = request.ContentType,
            ExtractedText = extractedText
        };

        var summary = new Summary
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            Document = document,
            Content = summaryContent
        };

        await _summaryRepository.AddAsync(summary);

        return new SummarizeDocumentResult(summary.Id, summaryContent);
    }
}
