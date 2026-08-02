namespace DocBrief.Application.Interfaces;

public interface ISummaryService
{
    Task<string> SummarizeAsync(string text, string summaryMode, string outputLanguage, bool includeConceptMap = false);

    Task<string> FixConceptMapAsync(string brokenCode, string errorMessage);
}
