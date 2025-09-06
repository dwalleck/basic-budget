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
        if (moneyResult.IsT1)
        {
            // Handle money creation error
            var error = new ApiError(moneyResult.AsT1.Message, "INVALID_MONEY_INPUT");
            return new CreateAccountPayload(null, new[] { error });
        }

        var command = new CreateAccountCommand(
            input.AccountNumber,
            input.Name,
            input.AccountType,
            moneyResult.AsT0
        );

        var result = await mediator.Send(command, cancellationToken);

        return result.Match(
            account => new CreateAccountPayload(account, null),
            error => new CreateAccountPayload(null, new[] { new ApiError(error.Message, error.Code) })
        );
    }
}
