using System.Text.Json;

namespace TaskRunner.Core.Operations.Implementations;

/// <summary>
/// Sample synchronous operation that performs a calculation.
/// </summary>
public class CalculatorOperation : ISyncOperation
{
    public string OperationType => "calculator";
    public string DisplayName => "Calculator";

    public Task<OperationResult> ExecuteAsync(Dictionary<string, object?> parameters)
    {
        if (!TryGetString(parameters, "operation", out var operation))
        {
            return Task.FromResult(OperationResult.Failed("Missing 'operation' parameter"));
        }

        if (!TryGetDouble(parameters, "a", out var a))
        {
            return Task.FromResult(OperationResult.Failed("Missing or invalid 'a' parameter"));
        }

        if (!TryGetDouble(parameters, "b", out var b))
        {
            return Task.FromResult(OperationResult.Failed("Missing or invalid 'b' parameter"));
        }

        double result;
        try
        {
            result = operation.ToLower() switch
            {
                "add" => a + b,
                "subtract" => a - b,
                "multiply" => a * b,
                "divide" when b != 0 => a / b,
                "divide" => throw new DivideByZeroException("Cannot divide by zero"),
                _ => throw new ArgumentException($"Unknown operation: {operation}")
            };
        }
        catch (Exception ex)
        {
            return Task.FromResult(OperationResult.Failed(ex.Message));
        }

        return Task.FromResult(OperationResult.Successful(
            $"{a} {operation} {b} = {result}",
            new Dictionary<string, object?>
            {
                ["result"] = result,
                ["operation"] = operation,
                ["operandA"] = a,
                ["operandB"] = b
            }));
    }

    private static bool TryConvertToDouble(object? value, out double result)
    {
        result = 0;
        if (value == null) return false;

        try
        {
            result = Convert.ToDouble(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetString(Dictionary<string, object?> parameters, string key, out string value)
    {
        value = string.Empty;

        if (!parameters.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        if (raw is string s)
        {
            value = s;
            return !string.IsNullOrWhiteSpace(value);
        }

        if (raw is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                value = element.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            }

            value = element.ToString();
            return !string.IsNullOrWhiteSpace(value);
        }

        value = raw.ToString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetDouble(Dictionary<string, object?> parameters, string key, out double value)
    {
        value = 0;

        if (!parameters.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        if (raw is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number => element.TryGetDouble(out value),
                JsonValueKind.String => double.TryParse(element.GetString(), out value),
                _ => false
            };
        }

        return TryConvertToDouble(raw, out value);
    }
}
