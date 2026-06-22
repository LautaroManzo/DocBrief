using DocBrief.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DocBrief.Infrastructure.Parsers;

public class TextParser : IDocumentParser
{
    public async Task<string> ParseAsync(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        return await reader.ReadToEndAsync();
    }
}
