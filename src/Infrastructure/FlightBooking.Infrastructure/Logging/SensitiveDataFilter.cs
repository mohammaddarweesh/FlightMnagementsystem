using Serilog.Core;
using Serilog.Events;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FlightBooking.Infrastructure.Logging;

public class SensitiveDataFilter : ILogEventFilter
{
    private static readonly string[] SensitiveFields = 
    {
        "password", "confirmPassword", "currentPassword", "newPassword",
        "token", "accessToken", "refreshToken", "apiKey", "secret",
        "creditCard", "cardNumber", "cvv", "ssn", "socialSecurityNumber"
    };

    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"\b\d{3}-?\d{3}-?\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex CreditCardRegex = new(@"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", RegexOptions.Compiled);

    public bool IsEnabled(LogEvent logEvent)
    {
        // Filter all log events
        return true;
    }

    public void Filter(LogEvent logEvent)
    {
        // Process message template
        if (logEvent.MessageTemplate?.Text != null)
        {
            var sanitizedMessage = SanitizeText(logEvent.MessageTemplate.Text);
            if (sanitizedMessage != logEvent.MessageTemplate.Text)
            {
                // Create new message template with sanitized text
                var newTemplate = new MessageTemplate(sanitizedMessage, logEvent.MessageTemplate.Tokens);
                // Note: LogEvent is immutable, so we can't modify it directly
                // The filtering will be applied to properties below
            }
        }

        // Process properties
        var propertiesToUpdate = new List<(string key, LogEventPropertyValue value)>();

        foreach (var property in logEvent.Properties)
        {
            var sanitizedValue = SanitizeProperty(property.Value);
            if (sanitizedValue != property.Value)
            {
                propertiesToUpdate.Add((property.Key, sanitizedValue));
            }
        }

        // Update properties if needed
        foreach (var (key, value) in propertiesToUpdate)
        {
            logEvent.AddOrUpdateProperty(new LogEventProperty(key, value));
        }
    }

    private LogEventPropertyValue SanitizeProperty(LogEventPropertyValue propertyValue)
    {
        return propertyValue switch
        {
            ScalarValue scalar when scalar.Value is string stringValue => 
                new ScalarValue(SanitizeText(stringValue)),
            
            StructureValue structure => 
                new StructureValue(structure.Properties.Select(p => 
                    new LogEventProperty(p.Name, SanitizeProperty(p.Value))), structure.TypeTag),
            
            SequenceValue sequence => 
                new SequenceValue(sequence.Elements.Select(SanitizeProperty)),
            
            DictionaryValue dictionary => 
                new DictionaryValue(dictionary.Elements.Select(kvp => 
                    new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                        kvp.Key, 
                        SanitizeProperty(kvp.Value)))),
            
            _ => propertyValue
        };
    }

    private string SanitizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sanitized = text;

        // Check for sensitive field patterns in JSON
        if (text.Contains('{') && text.Contains('}'))
        {
            sanitized = SanitizeJsonString(sanitized);
        }

        // Mask email addresses (keep domain for debugging)
        sanitized = EmailRegex.Replace(sanitized, match =>
        {
            var email = match.Value;
            var atIndex = email.IndexOf('@');
            if (atIndex > 0)
            {
                var localPart = email[..atIndex];
                var domain = email[atIndex..];
                var maskedLocal = localPart.Length > 2 
                    ? localPart[..2] + "***" 
                    : "***";
                return maskedLocal + domain;
            }
            return "***@***";
        });

        // Mask phone numbers
        sanitized = PhoneRegex.Replace(sanitized, "***-***-****");

        // Mask credit card numbers
        sanitized = CreditCardRegex.Replace(sanitized, "****-****-****-****");

        return sanitized;
    }

    private string SanitizeJsonString(string jsonString)
    {
        try
        {
            var jsonDoc = JsonDocument.Parse(jsonString);
            var sanitized = SanitizeJsonElement(jsonDoc.RootElement);
            return JsonSerializer.Serialize(sanitized);
        }
        catch
        {
            // If not valid JSON, return as-is
            return jsonString;
        }
    }

    private object SanitizeJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object>();
                foreach (var property in element.EnumerateObject())
                {
                    if (SensitiveFields.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        obj[property.Name] = "[REDACTED]";
                    }
                    else
                    {
                        obj[property.Name] = SanitizeJsonElement(property.Value);
                    }
                }
                return obj;

            case JsonValueKind.Array:
                return element.EnumerateArray().Select(SanitizeJsonElement).ToArray();

            case JsonValueKind.String:
                var stringValue = element.GetString() ?? "";
                return SanitizeText(stringValue);

            case JsonValueKind.Number:
                return element.GetDecimal();

            case JsonValueKind.True:
            case JsonValueKind.False:
                return element.GetBoolean();

            case JsonValueKind.Null:
                return null!;

            default:
                return element.ToString();
        }
    }
}
