using CSharpFunctionalExtensions;
using Shared.SharedKernel;

namespace Core.Abstractions;

public interface IQueryHandler<TResponse, in TQuery>
    where TQuery : IQuery
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default);
}

public interface IQueryHandlerWithResult<TResponse, in TQuery>
    where TQuery : IQuery
{
    Task<Result<TResponse, Error>> Handle(TQuery query, CancellationToken cancellationToken = default);
}
