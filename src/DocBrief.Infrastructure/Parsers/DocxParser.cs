using DocBrief.Application.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;

namespace DocBrief.Infrastructure.Parsers;

public class DocxParser : IDocumentParser
{
    public IReadOnlyCollection<string> SupportedExtensions { get; } = new[] { ".docx" };

    public async Task<string> ParseAsync(IFormFile file)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document.Body;

        return body?.InnerText.Trim() ?? string.Empty;
    }
}
