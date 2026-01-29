using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xplore.Domain.Entities;
using Xplore.Infrastructure.Persistence;

namespace Xplore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MuseumsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    // Iniettiamo il DbContext che abbiamo configurato prima
    public MuseumsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/museums
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var museums = await _context.Museums.ToListAsync();
        return Ok(museums);
    }

    // POST: api/museums
    [HttpPost]
    public async Task<IActionResult> Create(Museum museum)
    {
        // 1. Aggiungi al contesto
        _context.Museums.Add(museum);

        // 2. Salva nel DB vero (Commit)
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = museum.Id }, museum);
    }
}