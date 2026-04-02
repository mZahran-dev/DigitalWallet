namespace DigitalWallet.Models;

public record ParsedTransaction(
    string Reference,
    decimal Amount,
    DateTime Date,
    Dictionary<string, string>? Metadata = null
);
