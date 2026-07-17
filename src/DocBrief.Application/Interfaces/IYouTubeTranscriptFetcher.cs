namespace DocBrief.Application.Interfaces;

public interface IYouTubeTranscriptFetcher
{
    bool IsYouTubeUrl(string url);
    Task<string> FetchTranscriptAsync(string url);
}
