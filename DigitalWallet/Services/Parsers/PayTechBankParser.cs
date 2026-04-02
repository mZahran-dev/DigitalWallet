using DigitalWallet.Models;
using System.Globalization;

namespace DigitalWallet.Services.Parsers;

public class PayTechBankParser : IBankWebhookParser
{
    public string BankName => "PayTech";

    public IEnumerable<ParsedTransaction> Parse(string body)
    {
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            yield return ParseLine(line);
        }
    }

    private static ParsedTransaction ParseLine(string line)
    {
        // Split by '#' into 3 parts: DateAmount, Reference, KeyValuePairs
        var parts = line.Split('#', 3);
        if (parts.Length < 2)
            throw new FormatException($"Invalid PayTech transaction format: '{line}'");

        // Part 1: Date (8 chars YYYYMMDD) + Amount (comma as decimal separator)
        var dateAmountPart = parts[0];
        var dateString = dateAmountPart[..8];
        var amountString = dateAmountPart[8..];

        var date = DateTime.ParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture);
        var amount = decimal.Parse(amountString, new CultureInfo("fr-FR"));

        // Part 2: Reference
        var reference = parts[1];

        // Part 3 (optional): Key-value pairs separated by '/'
        Dictionary<string, string>? metadata = null;
        if (parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2]))
        {
            metadata = ParseKeyValuePairs(parts[2]);
        }

        return new ParsedTransaction(reference, amount, date, metadata);
    }
    private static Dictionary<string, string> ParseKeyValuePairs(string kvString)
    {
        var result = new Dictionary<string, string>();
        var tokens = kvString.Split('/');

        //values can contain spaces (not slashes), so each odd-index token is a value
        for (int i = 0; i < tokens.Length - 1; i += 2)
        {
            var key = tokens[i];
            var value = tokens[i + 1];
            result[key] = value;
        }

        return result;
    }
}
