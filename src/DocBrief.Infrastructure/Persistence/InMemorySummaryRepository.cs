using System.Collections.Concurrent;
using DocBrief.Application.Interfaces;
using DocBrief.Domain.Entities;

namespace DocBrief.Infrastructure.Persistence;

public class InMemorySummaryRepository : ISummaryRepository
{
    private readonly ConcurrentDictionary<Guid, Summary> _summaries = new();

    public Task<Summary> AddAsync(Summary summary)
    {
        _summaries[summary.Id] = summary;
        return Task.FromResult(summary);
    }

    public Task<Summary?> GetByIdAsync(Guid id)
    {
        _summaries.TryGetValue(id, out var summary);
        return Task.FromResult(summary);
    }

    public Task<IReadOnlyList<Summary>> GetAllAsync(int limit = 10)
    {
        var result = _summaries.Values
            .OrderByDescending(s => s.CreatedAt)
            .Take(limit)
            .ToList() as IReadOnlyList<Summary>;

        return Task.FromResult(result);
    }
}
