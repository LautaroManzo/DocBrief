namespace DocBrief.Application.Interfaces;

public interface IUrlContentFetcher
{
    Task<string> FetchTextAsync(string url);
}
