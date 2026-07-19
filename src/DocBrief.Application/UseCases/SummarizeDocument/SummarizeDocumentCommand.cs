using MediatR;
using Microsoft.AspNetCore.Http;

namespace DocBrief.Application.UseCases.SummarizeDocument;

public record SummarizeDocumentCommand(
    IFormFile? File,
    string? Text,
    string? Url,
    string ContentType,
    string SummaryMode,
    string OutputLanguage,
    bool IncludeConceptMap = false) : IRequest<SummarizeDocumentResult>;

public record SummarizeDocumentResult(string Summary, string? SourceTitle = null);
