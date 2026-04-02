namespace DigitalWallet.Models;

public class Transaction
{
    public int Id { get; set; }
    public required string Reference { get; set; }
    public required decimal Amount { get; set; }
    public required DateTime Date { get; set; }
    public required string BankName { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string? Metadata { get; set; }
}
