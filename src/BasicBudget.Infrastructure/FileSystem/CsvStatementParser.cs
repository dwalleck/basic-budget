using System.Globalization;

using BasicBudget.Application.Commands;
using BasicBudget.Domain.Errors;
using BasicBudget.Domain.ValueObjects;

using Microsoft.Extensions.Logging;

using OneOf;

namespace BasicBudget.Infrastructure.FileSystem;

public class CsvStatementParser : IStatementParser
{
    private readonly ILogger<CsvStatementParser> _logger;

    public CsvStatementParser(ILogger<CsvStatementParser> logger)
    {
        _logger = logger;
    }

    public async Task<OneOf<IReadOnlyList<ParsedTransactionData>, DomainError>> ParseAsync(
        string format,
        string content,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return new ValidationError("File format cannot be empty", "INVALID_FORMAT");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return new ValidationError("File content cannot be empty", "EMPTY_FILE_CONTENT");
        }

        try
        {
            return format.ToUpperInvariant() switch
            {
                "CSV" => await ParseCsvAsync(content, cancellationToken),
                "QFX" => await ParseQfxAsync(content, cancellationToken),
                "OFX" => await ParseOfxAsync(content, cancellationToken),
                _ => new ValidationError($"File format '{format}' is not supported. Supported formats: CSV, QFX, OFX", "UNSUPPORTED_FORMAT")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse statement file with format: {Format}", format);
            return new InfrastructureError(
                "STATEMENT_PARSE_FAILED",
                $"Failed to parse statement file: {ex.Message}",
                ex
            );
        }
    }

    private async Task<OneOf<IReadOnlyList<ParsedTransactionData>, DomainError>> ParseCsvAsync(
        string csvContent,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Parsing CSV statement content");

        var transactions = new List<ParsedTransactionData>();
        var lines = csvContent.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length == 0)
        {
            return new ValidationError("CSV file contains no data", "EMPTY_CSV_FILE");
        }

        // Detect CSV format and header structure
        var formatDetection = DetectCsvFormat(lines);
        if (formatDetection.IsT1)
        {
            return formatDetection.AsT1;
        }

        var csvFormat = formatDetection.AsT0;
        var startLine = csvFormat.HasHeader ? 1 : 0;

        for (int i = startLine; i < lines.Length; i++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parseResult = await ParseCsvLineAsync(line, csvFormat, i + 1);
            if (parseResult.IsT0)
            {
                transactions.Add(parseResult.AsT0);
            }
            else
            {
                _logger.LogWarning("Failed to parse CSV line {LineNumber}: {Error}", i + 1, parseResult.AsT1.Message);
                // Continue parsing other lines instead of failing the entire import
            }
        }

        if (transactions.Count == 0)
        {
            return new ValidationError("No valid transactions found in the CSV file", "NO_VALID_TRANSACTIONS");
        }

        _logger.LogInformation("Successfully parsed {Count} transactions from CSV", transactions.Count);
        return transactions.AsReadOnly();
    }

    private async Task<OneOf<IReadOnlyList<ParsedTransactionData>, DomainError>> ParseQfxAsync(
        string qfxContent,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Parsing QFX statement content");

        // QFX parsing implementation would go here
        // For now, return not implemented error
        await Task.Delay(1, cancellationToken); // Prevent compiler warning
        return new ValidationError("QFX file parsing is not yet implemented", "QFX_NOT_IMPLEMENTED");
    }

    private async Task<OneOf<IReadOnlyList<ParsedTransactionData>, DomainError>> ParseOfxAsync(
        string ofxContent,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Parsing OFX statement content");

        // OFX parsing implementation would go here
        // For now, return not implemented error
        await Task.Delay(1, cancellationToken); // Prevent compiler warning

        return new ValidationError(
            "OFX_NOT_IMPLEMENTED",
            "OFX file parsing is not yet implemented"
        );
    }

    private static OneOf<CsvFormat, DomainError> DetectCsvFormat(string[] lines)
    {
        if (lines.Length == 0)
        {
            return new ValidationError("CSV file is empty", "EMPTY_CSV");
        }

        var firstLine = lines[0].ToLowerInvariant();
        var delimiter = DetectDelimiter(firstLine);
        var fields = firstLine.Split(delimiter);

        // Check for common CSV header patterns
        var hasHeader = IsHeaderLine(fields);

        if (hasHeader)
        {
            return new CsvFormat(
                Delimiter: delimiter,
                HasHeader: true,
                DateColumnIndex: FindColumnIndex(fields, ["date", "transaction date", "trans date", "posting date"]),
                DescriptionColumnIndex: FindColumnIndex(fields, ["description", "memo", "details", "reference"]),
                AmountColumnIndex: FindColumnIndex(fields, ["amount", "transaction amount", "debit", "credit"])
            );
        }

        // If no header, assume standard format: Date, Description, Amount
        if (fields.Length >= 3)
        {
            return new CsvFormat(
                Delimiter: delimiter,
                HasHeader: false,
                DateColumnIndex: 0,
                DescriptionColumnIndex: 1,
                AmountColumnIndex: 2
            );
        }
        return new ValidationError("Unable to detect CSV format. Expected at least 3 columns: Date, Description, Amount", "INVALID_CSV_FORMAT");
    }

    private static char DetectDelimiter(string line)
    {
        var commaCount = line.Count(c => c == ',');
        var semicolonCount = line.Count(c => c == ';');
        var tabCount = line.Count(c => c == '\t');

        if (commaCount >= semicolonCount && commaCount >= tabCount)
            return ',';
        if (semicolonCount >= tabCount)
            return ';';
        return '\t';
    }

    private static bool IsHeaderLine(string[] fields)
    {
        return fields.Any(field =>
            field.Contains("date") ||
            field.Contains("description") ||
            field.Contains("amount") ||
            field.Contains("memo") ||
            field.Contains("reference"));
    }

    private static int FindColumnIndex(string[] headers, string[] possibleNames)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var header = headers[i].Trim().ToLowerInvariant();
            if (possibleNames.Contains(header))
            {
                return i;
            }
        }
        return -1; // Not found
    }

    private static async Task<OneOf<ParsedTransactionData, DomainError>> ParseCsvLineAsync(
        string line,
        CsvFormat format,
        int lineNumber)
    {
        try
        {
            var fields = line.Split(format.Delimiter);

            if (fields.Length < 3)
            {
                return new ValidationError($"Line {lineNumber}: Expected at least 3 columns, found {fields.Length}", "INSUFFICIENT_COLUMNS");
            }

            // Parse date
            var dateText = GetFieldValue(fields, format.DateColumnIndex);
            if (!TryParseDate(dateText, out var transactionDate))
            {
                return new ValidationError($"Line {lineNumber}: Unable to parse date '{dateText}'", "INVALID_DATE_FORMAT");
            }

            // Parse description
            var description = GetFieldValue(fields, format.DescriptionColumnIndex);
            if (string.IsNullOrWhiteSpace(description))
            {
                return new ValidationError($"Line {lineNumber}: Transaction description is required", "MISSING_DESCRIPTION");
            }

            // Parse amount
            var amountText = GetFieldValue(fields, format.AmountColumnIndex);
            if (!TryParseAmount(amountText, out var amount))
            {
                return new ValidationError($"Line {lineNumber}: Unable to parse amount '{amountText}'", "INVALID_AMOUNT_FORMAT");
            }

            // For now, assume USD currency - could be enhanced to detect from file
            var moneyResult = Money.Create(amount, "USD"); if (moneyResult.IsT1) return new ValidationError(moneyResult.AsT1.Message, "INVALID_MONEY"); var money = moneyResult.AsT0;

            await Task.Delay(1); // Prevent compiler warning for async method

            return new ParsedTransactionData(money, description.Trim(), transactionDate);
        }
        catch (Exception ex)
        {
            return new InfrastructureError(
                "CSV_LINE_PARSE_ERROR",
                $"Line {lineNumber}: Failed to parse CSV line - {ex.Message}",
                ex
            );
        }
    }

    private static string GetFieldValue(string[] fields, int index)
    {
        if (index < 0 || index >= fields.Length)
        {
            return string.Empty;
        }

        var value = fields[index].Trim();

        // Remove surrounding quotes if present
        if (value.Length >= 2 && value.StartsWith("\"") && value.EndsWith("\""))
        {
            value = value[1..^1];
        }

        return value;
    }

    private static bool TryParseDate(string dateText, out DateTime date)
    {
        date = default;

        if (string.IsNullOrWhiteSpace(dateText))
        {
            return false;
        }

        // Try common date formats
        string[] formats = [
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "dd/MM/yyyy",
            "MM-dd-yyyy",
            "dd-MM-yyyy",
            "yyyy/MM/dd",
            "M/d/yyyy",
            "d/M/yyyy"
        ];

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(dateText, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return true;
            }
        }

        // Try general parsing as fallback
        return DateTime.TryParse(dateText, out date);
    }

    private static bool TryParseAmount(string amountText, out decimal amount)
    {
        amount = 0;

        if (string.IsNullOrWhiteSpace(amountText))
        {
            return false;
        }

        // Clean the amount string - remove currency symbols, spaces, etc.
        var cleanAmount = amountText
            .Replace("$", "")
            .Replace("€", "")
            .Replace("£", "")
            .Replace("¥", "")
            .Replace(" ", "")
            .Replace(",", ""); // Remove thousand separators

        return decimal.TryParse(cleanAmount, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture, out amount);
    }
}

// Internal record to hold CSV format detection results
internal record CsvFormat(
    char Delimiter,
    bool HasHeader,
    int DateColumnIndex,
    int DescriptionColumnIndex,
    int AmountColumnIndex
);