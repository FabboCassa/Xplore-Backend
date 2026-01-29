using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xplore.Contracts; 
using Xplore.Domain.Entities;
using Xplore.Infrastructure.Persistence;

namespace Xplore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MuseumsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    // Iniettiamo il DbContext che abbiamo configurato prima
    public MuseumsController(ApplicationDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
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
        //pubblichiamo evento su rabbitmq
        await _publishEndpoint.Publish(new MuseumCreatedEvent(museum.Id, museum.Name));

        return CreatedAtAction(nameof(GetAll), new { id = museum.Id }, museum);
    }
}