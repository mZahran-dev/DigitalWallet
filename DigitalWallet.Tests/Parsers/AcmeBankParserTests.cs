using DigitalWallet.Services.Parsers;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DigitalWallet.Tests.Parsers;

public class AcmeBankParserTests
{
    private readonly AcmeBankParser _parser = new();

    [Fact]
    public void Parse_SingleTransaction_ReturnsCorrectValues()
    {
        var body = "156,50//202506159000001//20250615";

        var results = _parser.Parse(body).ToList();

        Assert.Single(results);
        var tx = results[0];
        Assert.Equal("202506159000001", tx.Reference);
        Assert.Equal(156.50m, tx.Amount);
        Assert.Equal(new DateTime(2025, 6, 15), tx.Date);
        Assert.Null(tx.Metadata);
    }

    [Fact]
    public void Parse_MultipleTransactions_ReturnsAll()
    {
        var body = "10,00//REF001//20250615\n200,75//REF002//20250616";

        var results = _parser.Parse(body).ToList();

        Assert.Equal(2, results.Count);
        Assert.Equal("REF001", results[0].Reference);
        Assert.Equal(10.00m, results[0].Amount);
        Assert.Equal("REF002", results[1].Reference);
        Assert.Equal(200.75m, results[1].Amount);
    }

    [Fact]
    public void Parse_InvalidFormat_ThrowsFormatException()
    {
        var body = "invalid-data";
        Assert.Throws<FormatException>(() => _parser.Parse(body).ToList());
    }

    [Fact]
    public void BankName_ReturnsAcme()
    {
        Assert.Equal("Acme", _parser.BankName);
    }
}
