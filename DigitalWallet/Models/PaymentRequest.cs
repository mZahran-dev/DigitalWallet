namespace DigitalWallet.Models;

public class PaymentRequest
{
    public required string Reference { get; set; }
    public required DateTime Date { get; set; }
    public required decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string SenderAccountNumber { get; set; }
    public required string ReceiverBankCode { get; set; }
    public required string ReceiverAccountNumber { get; set; }
    public required string BeneficiaryName { get; set; }
    public List<string> Notes { get; set; } = [];
    public int PaymentType { get; set; } = 99;
    public string ChargeDetails { get; set; } = "SHA";
}
