using DigitalWallet.Models;
using System.Xml.Linq;

namespace DigitalWallet.Services;

public class PaymentXmlBuilder : IPaymentXmlBuilder
{
    public string BuildXml(PaymentRequest request)
    {
        var root = new XElement("PaymentRequestMessage",
            new XElement("TransferInfo",
                new XElement("Reference", request.Reference),
                new XElement("Date", request.Date.ToString("yyyy-MM-dd HH:mm:sszzz")),
                new XElement("Amount", request.Amount.ToString("F2")),
                new XElement("Currency", request.Currency)
            ),
            new XElement("SenderInfo",
                new XElement("AccountNumber", request.SenderAccountNumber)
            ),
            new XElement("ReceiverInfo",
                new XElement("BankCode", request.ReceiverBankCode),
                new XElement("AccountNumber", request.ReceiverAccountNumber),
                new XElement("BeneficiaryName", request.BeneficiaryName)
            )
        );

        // Notes tag must not be present if there are no notes
        if (request.Notes.Count > 0)
        {
            root.Add(new XElement("Notes",
                request.Notes.Select(n => new XElement("Note", n))
            ));
        }

        // PaymentType tag must only be present if its value is other than 99
        if (request.PaymentType != 99)
        {
            root.Add(new XElement("PaymentType", request.PaymentType));
        }

        // ChargeDetails tag must only be present if its value is other than SHA
        if (!string.Equals(request.ChargeDetails, "SHA", StringComparison.OrdinalIgnoreCase))
        {
            root.Add(new XElement("ChargeDetails", request.ChargeDetails));
        }

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), root);

        using var sw = new StringWriter();
        doc.Save(sw);
        return sw.ToString();
    }
}
