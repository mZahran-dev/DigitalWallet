using DigitalWallet.Models;
using DigitalWallet.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly ITransactionIngestionService _ingestionService;

    public WebhookController(ITransactionIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    [HttpPost("{bankName}")]
    public async Task<IActionResult> ReceiveWebhook(string bankName, [FromBody] WebhookRequest request)
    {
        try
        {
            var imported = await _ingestionService.ProcessWebhookAsync(
                request.ClientId, bankName, request.Body);

            return Ok(new { imported, queued = imported == 0 && !string.IsNullOrEmpty(request.Body) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (FormatException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
