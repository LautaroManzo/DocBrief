using DocBrief.Domain.Entities;

namespace DocBrief.Application.Interfaces;

public interface ISummaryRepository
{
    Task<Summary> AddAsync(Summary summary);
    Task<Summary?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Summary>> GetAllAsync(int limit = 10);
}
