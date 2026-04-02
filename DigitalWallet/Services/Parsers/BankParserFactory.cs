namespace DigitalWallet.Services.Parsers;

public class BankParserFactory
{
    private readonly Dictionary<string, IBankWebhookParser> _parsers;

    public BankParserFactory(IEnumerable<IBankWebhookParser> parsers)
    {
        _parsers = parsers.ToDictionary(p => p.BankName, StringComparer.OrdinalIgnoreCase);
    }

    public IBankWebhookParser GetParser(string bankName)
    {
        if (_parsers.TryGetValue(bankName, out var parser))
            return parser;

        throw new ArgumentException($"No parser registered for bank '{bankName}'.");
    }

    public IReadOnlyCollection<string> SupportedBanks => _parsers.Keys.ToList().AsReadOnly();
}
