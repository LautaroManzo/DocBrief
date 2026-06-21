namespace DocBrief.Application.Interfaces;

public interface ISummaryService
{
    Task<string> SummarizeAsync(string text);
}
