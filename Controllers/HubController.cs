using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgroMove.API.Data;
using AgroMove.API.Models;
using Microsoft.AspNetCore.Authorization;

namespace AgroMove.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Only Admins should manage Hubs
    public class HubController : ControllerBase
    {
        private readonly AgroMoveDbContext _context;

        public HubController(AgroMoveDbContext context)
        {
            _context = context;
        }

        // GET: api/Hub
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Hub>>> GetHubs()
        {
            return await _context.Hubs.ToListAsync();
        }

        // POST: api/Hub
        [HttpPost]
        public async Task<ActionResult<Hub>> CreateHub(Hub hub)
        {
            hub.Id = Guid.NewGuid();
            _context.Hubs.Add(hub);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetHubs), new { id = hub.Id }, hub);
        }

        // PUT: api/Hub/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHub(Guid id, Hub hub)
        {
            if (id != hub.Id) return BadRequest();

            _context.Entry(hub).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HubExists(id)) return NotFound();
                else throw;
            }

            return NoContent();
        }

        // DELETE: api/Hub/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHub(Guid id)
        {
            var hub = await _context.Hubs.FindAsync(id);
            if (hub == null) return NotFound();

            _context.Hubs.Remove(hub);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private bool HubExists(Guid id) => _context.Hubs.Any(e => e.Id == id);
    }
}