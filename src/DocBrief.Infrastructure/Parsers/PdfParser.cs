using System.Text;
using DocBrief.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;

namespace DocBrief.Infrastructure.Parsers;

public class PdfParser : IDocumentParser
{
    public async Task<string> ParseAsync(IFormFile file)
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var document = PdfDocument.Open(stream);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        return sb.ToString().Trim();
    }
}
