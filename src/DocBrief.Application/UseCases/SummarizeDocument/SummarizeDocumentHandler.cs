using DocBrief.Application.Interfaces;
using MediatR;

namespace DocBrief.Application.UseCases.SummarizeDocument;

public class SummarizeDocumentHandler : IRequestHandler<SummarizeDocumentCommand, SummarizeDocumentResult>
{
    private readonly IDocumentParserResolver _parserResolver;
    private readonly IUrlContentFetcher _urlContentFetcher;
    private readonly IYouTubeTranscriptFetcher _youTubeTranscriptFetcher;
    private readonly ISummaryService _summaryService;

    public SummarizeDocumentHandler(
        IDocumentParserResolver parserResolver,
        IUrlContentFetcher urlContentFetcher,
        IYouTubeTranscriptFetcher youTubeTranscriptFetcher,
        ISummaryService summaryService)
    {
        _parserResolver = parserResolver;
        _urlContentFetcher = urlContentFetcher;
        _youTubeTranscriptFetcher = youTubeTranscriptFetcher;
        _summaryService = summaryService;
    }

    public async Task<SummarizeDocumentResult> Handle(SummarizeDocumentCommand request, CancellationToken cancellationToken)
    {
        string extractedText;

        try
        {
            extractedText = request.ContentType switch
            {
                "file" => await _parserResolver.Resolve(request.File!.FileName).ParseAsync(request.File!),
                "text" => request.Text!,
                "url" => await FetchUrlContentAsync(request.Url!),
                _ => throw new ArgumentException($"Unsupported content type: {request.ContentType}")
            };
        }
        catch (Exception ex) when (request.ContentType == "file" && ex is not ArgumentException)
        {
            throw new ArgumentException("No pudimos leer ese archivo. Verificá que no este daniado o corrupto.", ex);
        }

        var summaryContent = await _summaryService.SummarizeAsync(extractedText, request.SummaryMode, request.OutputLanguage);

        return new SummarizeDocumentResult(summaryContent);
    }

    private Task<string> FetchUrlContentAsync(string url) =>
        _youTubeTranscriptFetcher.IsYouTubeUrl(url)
            ? _youTubeTranscriptFetcher.FetchTranscriptAsync(url)
            : _urlContentFetcher.FetchTextAsync(url);
}
