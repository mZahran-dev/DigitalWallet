# Digital Wallet Challenge

A .NET 10 Web API application that implements an online wallet system capable of receiving money via bank webhooks and generating XML payment messages for sending money.

## Architecture & Design Decisions

### Receiving Money — Webhook Ingestion

**Strategy Pattern for Bank Parsers**

Each bank has a unique webhook format. The `IBankWebhookParser` interface defines a contract, and each bank gets its own implementation (`PayTechBankParser`, `AcmeBankParser`). The `BankParserFactory` resolves the correct parser by bank name. Adding a new bank is a matter of creating a new parser class and registering it in DI — no existing code needs modification (Open/Closed Principle).

**Duplicate Transaction Handling**

The `Transaction.Reference` column has a unique index in the database. Before inserting, the ingestion service bulk-fetches existing references from the incoming batch and filters out duplicates in memory. This avoids per-row database lookups and handles the requirement that the bank may report the same transaction multiple times with no effect on final state.

**Pause/Resume Ingestion**

The `IngestionControlService` provides a thread-safe toggle (`volatile bool`) for pausing ingestion. When paused, incoming webhooks are accepted (HTTP 200) and stored in a `ConcurrentQueue` in memory — they are not dropped. When resumed, the queue is drained and processed. This satisfies the requirement to stop processing without dropping incoming webhooks.

> **Note:** In a production system, the queue would be backed by a persistent store (e.g., a database table or message queue) rather than in-memory, to survive application restarts.

**Efficiency & Scaling**

- Batch deduplication: all incoming references are checked against the database in a single query.
- `IDbContextFactory` is used so the ingestion service creates short-lived DbContext instances, avoiding concurrency issues.
- A performance test verifies that 1,000 transactions are parsed and ingested within 5 seconds.

### Sending Money — XML Generation

The `PaymentXmlBuilder` uses `System.Xml.Linq` to build the XML document. Conditional elements follow the spec:

- **Notes** tag is omitted when there are no notes.
- **PaymentType** tag is omitted when the value is 99 (default).
- **ChargeDetails** tag is omitted when the value is "SHA" (default).

The `PaymentRequest` DTO defaults `PaymentType` to 99 and `ChargeDetails` to "SHA", so callers only need to set these when they differ from defaults.

### Project Structure

```
DigitalWalletChallenge/
├── Controllers/
│   ├── ClientsController.cs       # Client CRUD
│   ├── IngestionController.cs     # Pause/resume ingestion
│   ├── PaymentController.cs       # XML payment generation
│   └── WebhookController.cs       # Bank webhook receiver
├── Data/
│   └── WalletDbContext.cs         # EF Core context
├── Models/
│   ├── Client.cs                  # Client entity
│   ├── Transaction.cs             # Transaction entity
│   ├── ParsedTransaction.cs       # Parser output DTO
│   ├── PaymentRequest.cs          # Payment XML input DTO
│   └── WebhookRequest.cs          # Webhook payload DTO
├── Services/
│   ├── Parsers/
│   │   ├── IBankWebhookParser.cs  # Parser strategy interface
│   │   ├── PayTechBankParser.cs   # PayTech format parser
│   │   ├── AcmeBankParser.cs      # Acme format parser
│   │   └── BankParserFactory.cs   # Parser resolver
│   ├── IngestionControlService.cs # Pause/resume toggle
│   ├── ITransactionIngestionService.cs
│   ├── TransactionIngestionService.cs
│   ├── IPaymentXmlBuilder.cs
│   └── PaymentXmlBuilder.cs
└── Program.cs                     # DI configuration & startup

DigitalWalletChallenge.Tests/
├── Parsers/
│   ├── PayTechBankParserTests.cs  # PayTech parser unit tests
│   └── AcmeBankParserTests.cs     # Acme parser unit tests
├── Services/
│   ├── PaymentXmlBuilderTests.cs  # XML builder unit tests
│   └── TransactionIngestionServiceTests.cs  # Ingestion service tests
└── Integration/
    └── WebhookIntegrationTests.cs # Full API integration tests
```

## API Endpoints

### Webhooks

```
POST /api/webhooks/{bankName}
Body: { "clientId": 1, "body": "<raw webhook text>" }
```

### Ingestion Control

```
GET  /api/ingestion/status        # Check if ingestion is active
POST /api/ingestion/pause         # Pause processing (queue incoming)
POST /api/ingestion/resume        # Resume and process queued webhooks
```

### Payment XML

```
POST /api/payments/xml
Body: PaymentRequest JSON → Returns XML response
```

### Clients

```
POST /api/clients                 # Create a client
GET  /api/clients/{id}            # Get client with transactions
```

## Running

```bash
dotnet run --project DigitalWalletChallenge
```

## Testing

```bash
dotnet test
```

27 tests covering:
- Parser correctness for both bank formats
- Duplicate detection
- Pause/resume queue behavior
- XML conditional element generation
- Full API integration tests
- Performance test with 1,000 transactions
