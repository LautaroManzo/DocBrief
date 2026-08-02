using DocBrief.Application.Interfaces;
using MediatR;

namespace DocBrief.Application.UseCases.FixConceptMap;

public class FixConceptMapHandler : IRequestHandler<FixConceptMapCommand, FixConceptMapResult>
{
    private readonly ISummaryService _summaryService;

    public FixConceptMapHandler(ISummaryService summaryService)
    {
        _summaryService = summaryService;
    }

    public async Task<FixConceptMapResult> Handle(FixConceptMapCommand request, CancellationToken cancellationToken)
    {
        var fixedCode = await _summaryService.FixConceptMapAsync(request.Code, request.ErrorMessage);
        return new FixConceptMapResult(fixedCode);
    }
}
