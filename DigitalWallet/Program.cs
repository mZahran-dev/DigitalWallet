
using DigitalWallet.Data;
using DigitalWallet.Services;
using DigitalWallet.Services.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DigitalWallet;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        // Database
        builder.Services.AddDbContext<WalletDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
                             ?? "Data Source=wallet.db"));
        builder.Services.AddDbContextFactory<WalletDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
                             ?? "Data Source=wallet.db"), ServiceLifetime.Scoped);

        // Bank parsers — register each implementation; add new banks here
        builder.Services.AddSingleton<IBankWebhookParser, PayTechBankParser>();
        builder.Services.AddSingleton<IBankWebhookParser, AcmeBankParser>();
        builder.Services.AddSingleton<BankParserFactory>();

        // Services
        builder.Services.AddSingleton<IngestionControlService>();
        builder.Services.AddScoped<ITransactionIngestionService, TransactionIngestionService>();
        builder.Services.AddSingleton<IPaymentXmlBuilder, PaymentXmlBuilder>();

        var app = builder.Build();

        // Auto-create database on startup
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<WalletDbContext>();
            db.Database.EnsureCreated();
        }
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", "Digital Wallet API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
