using DigitalWallet.Data;
using DigitalWallet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DigitalWallet.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly WalletDbContext _db;

    public ClientsController(WalletDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClientRequest request)
    {
        var client = new Client { Name = request.Name };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = client.Id }, client);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var client = await _db.Clients
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client is null)
            return NotFound();

        return Ok(client);
    }
}

public class CreateClientRequest
{
    public required string Name { get; set; }
}
