using Microsoft.AspNetCore.Http;

namespace DocBrief.Application.Interfaces;

public interface IDocumentParser
{
    Task<string> ParseAsync(IFormFile file);
}
