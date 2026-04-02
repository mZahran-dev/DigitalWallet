using DigitalWallet.Data;
using DigitalWallet.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Xunit;

namespace DigitalWallet.Tests.Integration;

public class WebhookIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public WebhookIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove ALL EF-related service registrations to avoid dual-provider conflict
                var efDescriptors = services
                    .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                             || d.ServiceType == typeof(DbContextOptions<WalletDbContext>)
                             || d.ServiceType == typeof(IDbContextFactory<WalletDbContext>)
                             || d.ServiceType == typeof(WalletDbContext))
                    .ToList();
                foreach (var d in efDescriptors)
                    services.Remove(d);

                services.AddDbContext<WalletDbContext>(options =>
                    options.UseInMemoryDatabase(dbName));
                services.AddDbContextFactory<WalletDbContext>(options =>
                    options.UseInMemoryDatabase(dbName), ServiceLifetime.Scoped);
            });
        });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
        if (!db.Clients.Any())
        {
            db.Clients.Add(new Client { Name = "Integration Test Client" });
            db.SaveChanges();
        }
    }

    [Fact]
    public async Task Webhook_PayTech_ImportsTransaction()
    {
        var response = await _client.PostAsJsonAsync("/api/webhooks/PayTech", new WebhookRequest
        {
            ClientId = 1,
            Body = "20250615156,50#INTREF001#note/test"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_Acme_ImportsTransaction()
    {
        var response = await _client.PostAsJsonAsync("/api/webhooks/Acme", new WebhookRequest
        {
            ClientId = 1,
            Body = "156,50//INTREF002//20250615"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_UnknownBank_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/webhooks/UnknownBank", new WebhookRequest
        {
            ClientId = 1,
            Body = "some data"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task IngestionPauseResume_WorksCorrectly()
    {
        var pauseResponse = await _client.PostAsync("/api/ingestion/pause", null);
        Assert.Equal(HttpStatusCode.OK, pauseResponse.StatusCode);

        var webhookResponse = await _client.PostAsJsonAsync("/api/webhooks/PayTech", new WebhookRequest
        {
            ClientId = 1,
            Body = "2025061510,00#INTPAUSEREF001#note/paused"
        });
        Assert.Equal(HttpStatusCode.OK, webhookResponse.StatusCode);

        var resumeResponse = await _client.PostAsync("/api/ingestion/resume", null);
        Assert.Equal(HttpStatusCode.OK, resumeResponse.StatusCode);
    }

    [Fact]
    public async Task Payment_GeneratesValidXml()
    {
        var request = new PaymentRequest
        {
            Reference = "test-ref-001",
            Date = new DateTime(2025, 2, 25, 6, 33, 0),
            Amount = 177.39m,
            Currency = "SAR",
            SenderAccountNumber = "SA6980000204608016212908",
            ReceiverBankCode = "FDCSSARI",
            ReceiverAccountNumber = "SA6980000204608016211111",
            BeneficiaryName = "Jane Doe",
            Notes = ["Test Note"],
            PaymentType = 421,
            ChargeDetails = "RB"
        };

        var response = await _client.PostAsJsonAsync("/api/payments/xml", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("<PaymentRequestMessage>", content);
        Assert.Contains("<Reference>test-ref-001</Reference>", content);
        Assert.Contains("<PaymentType>421</PaymentType>", content);
    }
}
