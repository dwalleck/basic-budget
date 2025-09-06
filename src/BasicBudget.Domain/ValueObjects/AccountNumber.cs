using OneOf;
using System.Text.RegularExpressions;
using BasicBudget.Domain.Errors;

namespace BasicBudget.Domain.ValueObjects;

public record AccountNumber
{
    public string Value { get; init; }
    public string MaskedValue => CreateMaskedValue();
    
    private AccountNumber(string value)
    {
        Value = value;
    }
    
    public static OneOf<AccountNumber, DomainError> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new InvalidAccountNumberError(value ?? "");
            
        var normalized = NormalizeAccountNumber(value);
        
        if (normalized.Length < 8 || normalized.Length > 20)
            return new InvalidAccountNumberError(value);
            
        if (!IsAlphanumeric(normalized))
            return new InvalidAccountNumberError(value);
            
        return new AccountNumber(normalized);
    }
    
    private static string NormalizeAccountNumber(string value)
    {
        return Regex.Replace(value.Trim(), @"[\s\-]", "").ToUpperInvariant();
    }
    
    private static bool IsAlphanumeric(string value)
    {
        return Regex.IsMatch(value, @"^[A-Z0-9]+$");
    }
    
    private string CreateMaskedValue()
    {
        if (Value.Length <= 4)
            return Value;
            
        if (Value.Length <= 8)
            return $"****{Value[^4..]}";
            
        var visibleStart = Math.Min(4, Value.Length - 4);
        var maskedMiddle = new string('*', Value.Length - visibleStart - 4);
        return $"{Value[..visibleStart]}{maskedMiddle}{Value[^4..]}";
    }
    
    public virtual bool Equals(AccountNumber? other)
    {
        return other is not null && Value == other.Value;
    }
    
    public override int GetHashCode()
    {
        return Value.GetHashCode(StringComparison.OrdinalIgnoreCase);
    }
    
    public static implicit operator string(AccountNumber accountNumber) => accountNumber.Value;
    
    public override string ToString() => Value;
}

