using DigitalWallet.Data;
using DigitalWallet.Models;
using DigitalWallet.Services.Parsers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Text.Json;

namespace DigitalWallet.Services;

public class TransactionIngestionService : ITransactionIngestionService
{
    private readonly IDbContextFactory<WalletDbContext> _dbFactory;
    private readonly BankParserFactory _parserFactory;
    private readonly IngestionControlService _ingestionControl;

    // Thread-safe queue for webhooks received while ingestion is paused
    private static readonly ConcurrentQueue<QueuedWebhook> PendingWebhooks = new();

    public TransactionIngestionService(
        IDbContextFactory<WalletDbContext> dbFactory,
        BankParserFactory parserFactory,
        IngestionControlService ingestionControl)
    {
        _dbFactory = dbFactory;
        _parserFactory = parserFactory;
        _ingestionControl = ingestionControl;
    }

    public async Task<int> ProcessWebhookAsync(int clientId, string bankName, string body)
    {
        if (!_ingestionControl.IsIngestionActive)
        {
            // Queue the webhook for later — don't drop it
            PendingWebhooks.Enqueue(new QueuedWebhook(clientId, bankName, body));
            return 0;
        }

        return await IngestAsync(clientId, bankName, body);
    }

    public async Task<int> ProcessQueuedWebhooksAsync()
    {
        int totalImported = 0;

        while (PendingWebhooks.TryDequeue(out var queued))
        {
            totalImported += await IngestAsync(queued.ClientId, queued.BankName, queued.Body);
        }

        return totalImported;
    }

    private async Task<int> IngestAsync(int clientId, string bankName, string body)
    {
        var parser = _parserFactory.GetParser(bankName);
        var parsed = parser.Parse(body).ToList();

        if (parsed.Count == 0)
            return 0;

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Bulk-fetch existing references to skip duplicates efficiently
        var incomingRefs = parsed.Select(p => p.Reference).ToHashSet();
        var existingRefs = await db.Transactions
            .Where(t => incomingRefs.Contains(t.Reference))
            .Select(t => t.Reference)
            .ToHashSetAsync();

        var newTransactions = parsed
            .Where(p => !existingRefs.Contains(p.Reference))
            .Select(p => new Transaction
            {
                Reference = p.Reference,
                Amount = p.Amount,
                Date = p.Date,
                BankName = bankName,
                ClientId = clientId,
                Metadata = p.Metadata != null ? JsonSerializer.Serialize(p.Metadata) : null
            })
            .ToList();

        if (newTransactions.Count == 0)
            return 0;

        db.Transactions.AddRange(newTransactions);
        await db.SaveChangesAsync();

        return newTransactions.Count;
    }

    private record QueuedWebhook(int ClientId, string BankName, string Body);
}

