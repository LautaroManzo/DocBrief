using MediatR;

namespace DocBrief.Application.UseCases.FixConceptMap;

public record FixConceptMapCommand(string Code, string ErrorMessage) : IRequest<FixConceptMapResult>;

public record FixConceptMapResult(string Code);
