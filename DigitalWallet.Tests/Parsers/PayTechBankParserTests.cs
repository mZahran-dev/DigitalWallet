using DigitalWallet.Services.Parsers;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DigitalWallet.Tests.Parsers;

public class PayTechBankParserTests
{
    private readonly PayTechBankParser _parser = new();

    [Fact]
    public void Parse_SingleTransaction_ReturnsCorrectValues()
    {
        var body = "20250615156,50#202506159000001#note/debt payment march/internal_reference/A462JE81";

        var results = _parser.Parse(body).ToList();

        Assert.Single(results);
        var tx = results[0];
        Assert.Equal("202506159000001", tx.Reference);
        Assert.Equal(156.50m, tx.Amount);
        Assert.Equal(new DateTime(2025, 6, 15), tx.Date);
        Assert.NotNull(tx.Metadata);
        Assert.Equal("debt payment march", tx.Metadata!["note"]);
        Assert.Equal("A462JE81", tx.Metadata["internal_reference"]);
    }

    [Fact]
    public void Parse_MultipleTransactions_ReturnsAll()
    {
        var body = "2025061510,00#REF001#note/test\n20250616200,75#REF002#note/second";

        var results = _parser.Parse(body).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("REF001", results[0].Reference);
        Assert.Equal("REF002", results[1].Reference);
    }

    [Fact]
    public void Parse_TransactionWithoutMetadata_ReturnsNullMetadata()
    {
        var body = "20250615156,50#202506159000001";

        var results = _parser.Parse(body).ToList();

        Assert.Single(results);
        Assert.Null(results[0].Metadata);
    }

    [Fact]
    public void Parse_InvalidFormat_ThrowsFormatException()
    {
        var body = "invalid-data";
        Assert.Throws<FormatException>(() => _parser.Parse(body).ToList());
    }

    [Fact]
    public void BankName_ReturnsPayTech()
    {
        Assert.Equal("PayTech", _parser.BankName);
    }
}
