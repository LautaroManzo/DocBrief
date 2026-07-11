namespace DocBrief.Application.Interfaces;

public interface IDocumentParserResolver
{
    IDocumentParser Resolve(string fileName);
}
