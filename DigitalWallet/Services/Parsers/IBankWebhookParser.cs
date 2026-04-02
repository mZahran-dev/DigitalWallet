using DigitalWallet.Models;

namespace DigitalWallet.Services.Parsers;

public interface IBankWebhookParser
{
    string BankName { get; }
    IEnumerable<ParsedTransaction> Parse(string body);
}
