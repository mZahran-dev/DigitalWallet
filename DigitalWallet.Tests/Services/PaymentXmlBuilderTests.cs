using DigitalWallet.Models;
using DigitalWallet.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Xunit;

namespace DigitalWallet.Tests.Services;

public class PaymentXmlBuilderTests
{
    private readonly PaymentXmlBuilder _builder = new();

    private static PaymentRequest CreateFullRequest() => new()
    {
        Reference = "e0f4763d-28ea-42d4-ac1c-c4013c242105",
        Date = new DateTime(2025, 2, 25, 6, 33, 0),
        Amount = 177.39m,
        Currency = "SAR",
        SenderAccountNumber = "SA6980000204608016212908",
        ReceiverBankCode = "FDCSSARI",
        ReceiverAccountNumber = "SA6980000204608016211111",
        BeneficiaryName = "Jane Doe",
        Notes = ["Lorem Epsum", "Dolor Sit Amet"],
        PaymentType = 421,
        ChargeDetails = "RB"
    };

    [Fact]
    public void BuildXml_FullRequest_ContainsAllElements()
    {
        var xml = _builder.BuildXml(CreateFullRequest());
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        Assert.Equal("PaymentRequestMessage", root.Name.LocalName);
        Assert.Equal("e0f4763d-28ea-42d4-ac1c-c4013c242105", root.Element("TransferInfo")!.Element("Reference")!.Value);
        Assert.Equal("177.39", root.Element("TransferInfo")!.Element("Amount")!.Value);
        Assert.Equal("SAR", root.Element("TransferInfo")!.Element("Currency")!.Value);
        Assert.Equal("SA6980000204608016212908", root.Element("SenderInfo")!.Element("AccountNumber")!.Value);
        Assert.Equal("FDCSSARI", root.Element("ReceiverInfo")!.Element("BankCode")!.Value);
        Assert.Equal("SA6980000204608016211111", root.Element("ReceiverInfo")!.Element("AccountNumber")!.Value);
        Assert.Equal("Jane Doe", root.Element("ReceiverInfo")!.Element("BeneficiaryName")!.Value);

        var notes = root.Element("Notes")!.Elements("Note").Select(n => n.Value).ToList();
        Assert.Equal(2, notes.Count);
        Assert.Equal("Lorem Epsum", notes[0]);
        Assert.Equal("Dolor Sit Amet", notes[1]);

        Assert.Equal("421", root.Element("PaymentType")!.Value);
        Assert.Equal("RB", root.Element("ChargeDetails")!.Value);
    }

    [Fact]
    public void BuildXml_NoNotes_NotesElementAbsent()
    {
        var request = CreateFullRequest();
        request.Notes = [];

        var xml = _builder.BuildXml(request);
        var doc = XDocument.Parse(xml);

        Assert.Null(doc.Root!.Element("Notes"));
    }

    [Fact]
    public void BuildXml_DefaultPaymentType99_PaymentTypeElementAbsent()
    {
        var request = CreateFullRequest();
        request.PaymentType = 99;

        var xml = _builder.BuildXml(request);
        var doc = XDocument.Parse(xml);

        Assert.Null(doc.Root!.Element("PaymentType"));
    }

    [Fact]
    public void BuildXml_DefaultChargeDetailsSHA_ChargeDetailsElementAbsent()
    {
        var request = CreateFullRequest();
        request.ChargeDetails = "SHA";

        var xml = _builder.BuildXml(request);
        var doc = XDocument.Parse(xml);

        Assert.Null(doc.Root!.Element("ChargeDetails"));
    }

    [Fact]
    public void BuildXml_AllDefaults_MinimalXml()
    {
        var request = CreateFullRequest();
        request.Notes = [];
        request.PaymentType = 99;
        request.ChargeDetails = "SHA";

        var xml = _builder.BuildXml(request);
        var doc = XDocument.Parse(xml);
        var root = doc.Root!;

        Assert.Null(root.Element("Notes"));
        Assert.Null(root.Element("PaymentType"));
        Assert.Null(root.Element("ChargeDetails"));
        Assert.NotNull(root.Element("TransferInfo"));
        Assert.NotNull(root.Element("SenderInfo"));
        Assert.NotNull(root.Element("ReceiverInfo"));
    }
}
