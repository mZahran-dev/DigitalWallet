namespace DigitalWallet.Models;

public class WebhookRequest
{
    public required int ClientId { get; set; }
    public required string Body { get; set; }
}
