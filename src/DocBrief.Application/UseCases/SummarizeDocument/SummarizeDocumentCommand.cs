using MediatR;
using Microsoft.AspNetCore.Http;

namespace DocBrief.Application.UseCases.SummarizeDocument;

public record SummarizeDocumentCommand(
    IFormFile? File,
    string? Text,
    string ContentType,
    string SummaryLength,
    string OutputLanguage) : IRequest<SummarizeDocumentResult>;

public record SummarizeDocumentResult(string Summary, int OriginalWordCount);
