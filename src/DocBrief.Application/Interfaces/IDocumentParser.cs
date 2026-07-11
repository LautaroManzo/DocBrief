using Microsoft.AspNetCore.Http;

namespace DocBrief.Application.Interfaces;

public interface IDocumentParser
{
    IReadOnlyCollection<string> SupportedExtensions { get; }
    Task<string> ParseAsync(IFormFile file);
}
