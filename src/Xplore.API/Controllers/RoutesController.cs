using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xplore.Contracts.Routes;
using Xplore.Domain.Entities;
using Xplore.Infrastructure.Persistence;

namespace Xplore.API.Controllers;

/// <summary>
/// Controller for saved routes: create, list, complete, and share.
/// </summary>
[ApiController]
[Authorize]
[Route("api/routes")]
public class RoutesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RoutesController> _logger;

    public RoutesController(
        ApplicationDbContext dbContext,
        ILogger<RoutesController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Create a new saved route with waypoints.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SavedRouteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRoute([FromBody] CreateSavedRouteRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Il nome del percorso è obbligatorio.");

        if (request.Waypoints.Count < 2)
            return BadRequest("Il percorso deve avere almeno 2 tappe.");

        var route = new SavedRoute
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsCompleted = false,
            ShareToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow,
            Waypoints = request.Waypoints.Select(w => new SavedRouteWaypoint
            {
                Id = Guid.NewGuid(),
                PlaceId = w.PlaceId,
                Name = w.Name,
                Latitude = w.Latitude,
                Longitude = w.Longitude,
                OrderIndex = w.OrderIndex,
            }).ToList(),
        };

        _dbContext.SavedRoutes.Add(route);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created route {RouteId} '{RouteName}'", userId, route.Id, route.Name);

        return CreatedAtAction(nameof(GetRouteById), new { id = route.Id }, MapToResponse(route));
    }

    /// <summary>
    /// Get a single route by ID (must belong to current user).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SavedRouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRouteById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var route = await _dbContext.SavedRoutes
            .Include(r => r.Waypoints.OrderBy(w => w.OrderIndex))
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (route == null) return NotFound();

        return Ok(MapToResponse(route));
    }

    /// <summary>
    /// Get the current user's saved (not completed) routes.
    /// </summary>
    [HttpGet("saved")]
    [ProducesResponseType(typeof(List<SavedRouteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavedRoutes()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var routes = await _dbContext.SavedRoutes
            .Where(r => r.UserId == userId && !r.IsCompleted)
            .Include(r => r.Waypoints.OrderBy(w => w.OrderIndex))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(routes.Select(MapToResponse));
    }

    /// <summary>
    /// Get the current user's completed routes.
    /// </summary>
    [HttpGet("completed")]
    [ProducesResponseType(typeof(List<SavedRouteResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompletedRoutes()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var routes = await _dbContext.SavedRoutes
            .Where(r => r.UserId == userId && r.IsCompleted)
            .Include(r => r.Waypoints.OrderBy(w => w.OrderIndex))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(routes.Select(MapToResponse));
    }

    /// <summary>
    /// Mark a route as completed.
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRouteCompleted(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var route = await _dbContext.SavedRoutes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (route == null) return NotFound();

        route.IsCompleted = true;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} completed route {RouteId}", userId, id);
        return Ok();
    }

    /// <summary>
    /// Delete a saved route.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRoute(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var route = await _dbContext.SavedRoutes
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

        if (route == null) return NotFound();

        _dbContext.SavedRoutes.Remove(route);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted route {RouteId}", userId, id);
        return Ok();
    }

    /// <summary>
    /// Get a shared route by its share token. No authentication required.
    /// </summary>
    [HttpGet("shared/{shareToken}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SavedRouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSharedRoute(string shareToken)
    {
        var route = await _dbContext.SavedRoutes
            .Include(r => r.Waypoints.OrderBy(w => w.OrderIndex))
            .FirstOrDefaultAsync(r => r.ShareToken == shareToken);

        if (route == null) return NotFound();

        return Ok(MapToResponse(route));
    }

    // ── Helpers ──

    private static SavedRouteResponse MapToResponse(SavedRoute r) =>
        new(r.Id, r.UserId, r.Name, r.Description, r.IsCompleted, r.ShareToken, r.CreatedAt,
            r.Waypoints.OrderBy(w => w.OrderIndex).Select(w =>
                new SavedRouteWaypointResponse(w.Id, w.PlaceId, w.Name, w.Latitude, w.Longitude, w.OrderIndex)
            ).ToList());
}
