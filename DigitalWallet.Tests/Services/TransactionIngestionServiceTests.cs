using DigitalWallet.Data;
using DigitalWallet.Models;
using DigitalWallet.Services;
using DigitalWallet.Services.Parsers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DigitalWallet.Tests.Services;

public class TransactionIngestionServiceTests : IDisposable
{
    private readonly DbContextOptions<WalletDbContext> _options;
    private readonly WalletDbContext _db;
    private readonly TransactionIngestionService _service;
    private readonly IngestionControlService _ingestionControl;

    public TransactionIngestionServiceTests()
    {
        var dbName = Guid.NewGuid().ToString();
        _options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        _db = new WalletDbContext(_options);
        _db.Database.EnsureCreated();

        _db.Clients.Add(new Client { Id = 1, Name = "Test Client" });
        _db.SaveChanges();

        var factory = new TestDbContextFactory(_options);
        var parsers = new IBankWebhookParser[] { new PayTechBankParser(), new AcmeBankParser() };
        var parserFactory = new BankParserFactory(parsers);
        _ingestionControl = new IngestionControlService();

        _service = new TransactionIngestionService(factory, parserFactory, _ingestionControl);
    }

    [Fact]
    public async Task ProcessWebhook_NewTransactions_ImportsAll()
    {
        var body = "20250615100,00#REF001#note/test\n2025061650,25#REF002#note/test2";

        var count = await _service.ProcessWebhookAsync(1, "PayTech", body);

        Assert.Equal(2, count);
        await using var verifyDb = new WalletDbContext(_options);
        Assert.Equal(2, await verifyDb.Transactions.CountAsync());
    }

    [Fact]
    public async Task ProcessWebhook_DuplicateReference_SkipsDuplicates()
    {
        var body = "20250615100,00#DUPREF001#note/test";

        await _service.ProcessWebhookAsync(1, "PayTech", body);
        var secondCount = await _service.ProcessWebhookAsync(1, "PayTech", body);

        Assert.Equal(0, secondCount);
    }

    [Fact]
    public async Task ProcessWebhook_MixedNewAndDuplicate_ImportsOnlyNew()
    {
        await _service.ProcessWebhookAsync(1, "PayTech", "20250615100,00#MIXREF001#note/test");

        var body = "20250615100,00#MIXREF001#note/test\n2025061650,25#MIXREF002#note/test2";
        var count = await _service.ProcessWebhookAsync(1, "PayTech", body);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ProcessWebhook_WhenPaused_QueuesWebhook()
    {
        _ingestionControl.Pause();

        var count = await _service.ProcessWebhookAsync(1, "PayTech", "20250615100,00#PAUSEREF001#note/test");

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ProcessWebhook_PauseAndResume_ProcessesQueued()
    {
        // Use unique references to avoid collisions with other tests
        _ingestionControl.Pause();
        await _service.ProcessWebhookAsync(1, "PayTech", "20250615100,00#RESUMEQREF001#note/test");

        _ingestionControl.Resume();
        var count = await _service.ProcessQueuedWebhooksAsync();

        Assert.True(count >= 1);
    }

    [Fact]
    public async Task ProcessWebhook_AcmeFormat_ParsesCorrectly()
    {
        var body = "156,50//ACMEREF001//20250615";

        var count = await _service.ProcessWebhookAsync(1, "Acme", body);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task ProcessWebhook_UnknownBank_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.ProcessWebhookAsync(1, "UnknownBank", "data"));
    }

    [Fact]
    public async Task ProcessWebhook_1000Transactions_CompletesInReasonableTime()
    {
        var lines = Enumerable.Range(1, 1000)
            .Select(i => $"{i * 10:F2}//PERFREF{i:D5}//20250615"
                .Replace('.', ','));
        var body = string.Join("\n", lines);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var count = await _service.ProcessWebhookAsync(1, "Acme", body);
        sw.Stop();

        Assert.Equal(1000, count);
        Assert.True(sw.ElapsedMilliseconds < 5000,
            $"Processing 1000 transactions took {sw.ElapsedMilliseconds}ms, exceeding 5s threshold");
    }

    public void Dispose() => _db.Dispose();

    private class TestDbContextFactory : IDbContextFactory<WalletDbContext>
    {
        private readonly DbContextOptions<WalletDbContext> _options;
        public TestDbContextFactory(DbContextOptions<WalletDbContext> options) => _options = options;
        public WalletDbContext CreateDbContext() => new(_options);
    }
}
