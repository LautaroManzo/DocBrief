namespace DocBrief.Application.Interfaces;

public record UrlContent(string Text, string? Title);

public interface IUrlContentFetcher
{
    Task<UrlContent> FetchAsync(string url);
}
