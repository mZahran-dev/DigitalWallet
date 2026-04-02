using DigitalWallet.Models;

namespace DigitalWallet.Services;

public interface IPaymentXmlBuilder
{
    string BuildXml(PaymentRequest request);
}
