
namespace DigitalWallet.Models;

public class Client
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = [];
}
