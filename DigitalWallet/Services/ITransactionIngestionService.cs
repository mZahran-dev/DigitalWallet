namespace DigitalWallet.Services;

public interface ITransactionIngestionService
{
    // Processes a webhook payload: parses transactions and stores them
    Task<int> ProcessWebhookAsync(int clientId, string bankName, string body);
    Task<int> ProcessQueuedWebhooksAsync();
}
