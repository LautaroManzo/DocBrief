using MediatR;
using Microsoft.AspNetCore.Http;

namespace DocBrief.Application.UseCases.SummarizeDocument;

public record SummarizeDocumentCommand(
    IFormFile? File,
    string? Text,
    string? Url,
    string ContentType,
    string SummaryMode,
    string OutputLanguage) : IRequest<SummarizeDocumentResult>;

public record SummarizeDocumentResult(string Summary);
