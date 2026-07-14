using DocBrief.Application.Interfaces;
using MediatR;

namespace DocBrief.Application.UseCases.SummarizeDocument;

public class SummarizeDocumentHandler : IRequestHandler<SummarizeDocumentCommand, SummarizeDocumentResult>
{
    private readonly IDocumentParserResolver _parserResolver;
    private readonly IUrlContentFetcher _urlContentFetcher;
    private readonly ISummaryService _summaryService;

    public SummarizeDocumentHandler(
        IDocumentParserResolver parserResolver,
        IUrlContentFetcher urlContentFetcher,
        ISummaryService summaryService)
    {
        _parserResolver = parserResolver;
        _urlContentFetcher = urlContentFetcher;
        _summaryService = summaryService;
    }

    public async Task<SummarizeDocumentResult> Handle(SummarizeDocumentCommand request, CancellationToken cancellationToken)
    {
        var extractedText = request.ContentType switch
        {
            "file" => await _parserResolver.Resolve(request.File!.FileName).ParseAsync(request.File!),
            "text" => request.Text!,
            "url" => await _urlContentFetcher.FetchTextAsync(request.Url!),
            _ => throw new ArgumentException($"Unsupported content type: {request.ContentType}")
        };

        var summaryContent = await _summaryService.SummarizeAsync(extractedText, request.SummaryLength, request.OutputLanguage);
        var originalWordCount = extractedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return new SummarizeDocumentResult(summaryContent, originalWordCount);
    }
}
