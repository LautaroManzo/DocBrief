using DocBrief.Application.Interfaces;
using MediatR;

namespace DocBrief.Application.UseCases.SummarizeDocument;

public class SummarizeDocumentHandler : IRequestHandler<SummarizeDocumentCommand, SummarizeDocumentResult>
{
    private readonly IDocumentParserResolver _parserResolver;
    private readonly ISummaryService _summaryService;

    public SummarizeDocumentHandler(IDocumentParserResolver parserResolver, ISummaryService summaryService)
    {
        _parserResolver = parserResolver;
        _summaryService = summaryService;
    }

    public async Task<SummarizeDocumentResult> Handle(SummarizeDocumentCommand request, CancellationToken cancellationToken)
    {
        var extractedText = request.ContentType switch
        {
            "file" => await _parserResolver.Resolve(request.File!.FileName).ParseAsync(request.File!),
            "text" => request.Text!,
            _ => throw new ArgumentException($"Unsupported content type: {request.ContentType}")
        };

        var summaryContent = await _summaryService.SummarizeAsync(extractedText, request.SummaryLength, request.OutputLanguage);
        var originalWordCount = extractedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return new SummarizeDocumentResult(summaryContent, originalWordCount);
    }
}
