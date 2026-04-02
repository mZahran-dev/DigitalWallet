using DigitalWallet.Models;
using DigitalWallet.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Controllers;


[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentXmlBuilder _xmlBuilder;

    public PaymentController(IPaymentXmlBuilder xmlBuilder)
    {
        _xmlBuilder = xmlBuilder;
    }

    // Generates the XML payment message for sending money to another bank.
    [HttpPost("xml")]
    public IActionResult GenerateXml([FromBody] PaymentRequest request)
    {
        var xml = _xmlBuilder.BuildXml(request);
        return Content(xml, "application/xml");
    }
}
