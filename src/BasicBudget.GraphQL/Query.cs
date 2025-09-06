using BasicBudget.Application.Queries;
using BasicBudget.Domain.Entities;
using MediatR;

public class Query
{
    public async Task<Account?> GetAccountAsync(
        Guid id,
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAccountQuery(id), cancellationToken);
        return result.Match(
            account => account,
            error => null // Or throw a GraphQLException
        );
    }
}
