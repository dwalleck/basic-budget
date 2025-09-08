using BasicBudget.Application.Commands;
using BasicBudget.Domain.Entities;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.ValueObjects;
using MediatR;

// As per the schema, this will be our standard error format
public record ApiError(string Message, string Code);

// Input and Payload types for the createAccount mutation
public record CreateAccountInput(string AccountNumber, string Name, AccountType AccountType, MoneyInput InitialBalance);
public record MoneyInput(decimal Amount, string Currency);
public record CreateAccountPayload(Account? Account, IReadOnlyList<ApiError>? Errors);

public class Mutation
{
    public async Task<CreateAccountPayload> CreateAccountAsync(
        CreateAccountInput input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var moneyResult = Money.Create(input.InitialBalance.Amount, input.InitialBalance.Currency);

        return await moneyResult.Match(
            async money =>
            {
                var command = new CreateAccountCommand(
                    input.AccountNumber,
                    input.Name,
                    input.AccountType,
                    money
                );

                var result = await mediator.Send(command, cancellationToken);

                return result.Match(
                    account => new CreateAccountPayload(account, null),
                    error => new CreateAccountPayload(null, [new ApiError(error.Message, error.Code)])
                );
            },
            error => Task.FromResult(new CreateAccountPayload(null, [new ApiError(error.Message, "INVALID_MONEY_INPUT")]))
        );
    }
}
