using DigitalWallet.Services;
using Microsoft.AspNetCore.Mvc;

namespace DigitalWallet.Controllers;

[ApiController]
[Route("api/ingestion")]
public class IngestionController : ControllerBase
{
    private readonly IngestionControlService _controlService;
    private readonly ITransactionIngestionService _ingestionService;

    public IngestionController(
        IngestionControlService controlService,
        ITransactionIngestionService ingestionService)
    {
        _controlService = controlService;
        _ingestionService = ingestionService;
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { active = _controlService.IsIngestionActive });
    }

    [HttpPost("pause")]
    public IActionResult Pause()
    {
        _controlService.Pause();
        return Ok(new { active = false });
    }

    // Resumes ingestion and processes any webhooks that were queued while paused.
    [HttpPost("resume")]
    public async Task<IActionResult> Resume()
    {
        _controlService.Resume();
        var processed = await _ingestionService.ProcessQueuedWebhooksAsync();
        return Ok(new { active = true, processedFromQueue = processed });
    }
}
