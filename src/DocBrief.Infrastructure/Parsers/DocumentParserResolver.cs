using DocBrief.Application.Interfaces;

namespace DocBrief.Infrastructure.Parsers;

public class DocumentParserResolver : IDocumentParserResolver
{
    private readonly IEnumerable<IDocumentParser> _parsers;

    public DocumentParserResolver(IEnumerable<IDocumentParser> parsers)
    {
        _parsers = parsers;
    }

    public IDocumentParser Resolve(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        var parser = _parsers.FirstOrDefault(p => p.SupportedExtensions.Contains(extension));

        return parser ?? throw new NotSupportedException($"No hay un parser disponible para la extension '{extension}'.");
    }
}
