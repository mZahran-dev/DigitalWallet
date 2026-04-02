using DigitalWallet.Models;
using System.Globalization;

namespace DigitalWallet.Services.Parsers;

public class AcmeBankParser : IBankWebhookParser
{
    public string BankName => "Acme";

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
        var parts = line.Split("//");
        if (parts.Length != 3)
            throw new FormatException($"Invalid Acme transaction format: '{line}'");

        var amount = decimal.Parse(parts[0], new CultureInfo("fr-FR"));
        var reference = parts[1];
        var date = DateTime.ParseExact(parts[2], "yyyyMMdd", CultureInfo.InvariantCulture);

        return new ParsedTransaction(reference, amount, date);
    }
}

