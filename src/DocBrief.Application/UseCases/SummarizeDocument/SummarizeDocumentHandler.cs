using DocBrief.Application.Interfaces;
using MediatR;

namespace DocBrief.Application.UseCases.SummarizeDocument;

public class SummarizeDocumentHandler : IRequestHandler<SummarizeDocumentCommand, SummarizeDocumentResult>
{
    private readonly IDocumentParser _pdfParser;
    private readonly ISummaryService _summaryService;

    public SummarizeDocumentHandler(IDocumentParser pdfParser, ISummaryService summaryService)
    {
        _pdfParser = pdfParser;
        _summaryService = summaryService;
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

        return new SummarizeDocumentResult(summaryContent);
    }
}
