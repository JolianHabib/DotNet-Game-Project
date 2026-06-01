using GameServer.Data;
using GameServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PlayersController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlayer(int id)
        {
            var player = await _db.Players
                .Include(p => p.Country)
                .Select(p => new {
                    p.Id,
                    p.FirstName,
                    p.IdentityNumber,
                    p.Phone,
                    Country = p.Country.Name
                })
                .FirstOrDefaultAsync(p => p.IdentityNumber == id);

            if (player == null)
                return NotFound();

            return Ok(player);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlayer([FromBody] Player player)
        {
            bool exists = await _db.Players
                .AnyAsync(p => p.IdentityNumber == player.IdentityNumber);

            if (exists)
                return BadRequest("Identity number already exists.");

            _db.Players.Add(player);
            await _db.SaveChangesAsync();
            return Ok(player);
        }
    }
}