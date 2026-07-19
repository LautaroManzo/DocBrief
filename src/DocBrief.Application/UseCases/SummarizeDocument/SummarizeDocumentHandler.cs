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
        string? sourceTitle = null;

        try
        {
            switch (request.ContentType)
            {
                case "file":
                    extractedText = await _parserResolver.Resolve(request.File!.FileName).ParseAsync(request.File!);
                    break;
                case "text":
                    extractedText = request.Text!;
                    break;
                case "url":
                    (extractedText, sourceTitle) = await FetchUrlContentAsync(request.Url!);
                    break;
                default:
                    throw new ArgumentException($"Unsupported content type: {request.ContentType}");
            }
        }
        catch (Exception ex) when (request.ContentType == "file" && ex is not ArgumentException)
        {
            throw new ArgumentException("No pudimos leer ese archivo. Verificá que no este daniado o corrupto.", ex);
        }

        var summaryContent = await _summaryService.SummarizeAsync(
            extractedText, request.SummaryMode, request.OutputLanguage, request.IncludeConceptMap);

        return new SummarizeDocumentResult(summaryContent, sourceTitle);
    }

    private async Task<(string Text, string? Title)> FetchUrlContentAsync(string url)
    {
        if (_youTubeTranscriptFetcher.IsYouTubeUrl(url))
        {
            var transcript = await _youTubeTranscriptFetcher.FetchTranscriptAsync(url);
            var title = await _youTubeTranscriptFetcher.GetTitleAsync(url);
            return (transcript, title);
        }

        var content = await _urlContentFetcher.FetchAsync(url);
        return (content.Text, content.Title);
    }
}
