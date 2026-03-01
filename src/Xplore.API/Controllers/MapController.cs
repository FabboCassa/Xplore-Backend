using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xplore.Contracts.Map;
using Xplore.Domain.Entities;
using Xplore.Infrastructure.Map;
using Xplore.Infrastructure.Persistence;

namespace Xplore.API.Controllers;

/// <summary>
/// Stateless POI proxy controller.
/// </summary>
[ApiController]
[Route("api/map")]
public class MapController : ControllerBase
{
    private readonly OverpassApiService _overpassService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MapController> _logger;

    public MapController(OverpassApiService overpassService, ApplicationDbContext dbContext, ILogger<MapController> logger)
    {
        _overpassService = overpassService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("pois")]
    public async Task<ActionResult<List<MapPinResponse>>> GetPois(
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] double radius = 3.0)
    {
        _logger.LogInformation("[MapController] GET /api/map/pois?lat={Lat}&lon={Lon}&radius={Radius}km", lat, lon, radius);

        if (radius is <= 0 or > 50)
            return BadRequest("Radius must be between 0 and 50 km.");

        var pois = await _overpassService.GetPoisAsync(lat, lon, radius);
        
        var poiIds = pois.Select(p => p.Id).ToList();
        var ratings = await _dbContext.PoiRatings
            .Where(r => poiIds.Contains(r.PoiId))
            .GroupBy(r => r.PoiId)
            .Select(g => new { PoiId = g.Key, Avg = g.Average(r => (double)r.Score), Count = g.Count() })
            .ToDictionaryAsync(x => x.PoiId, x => x);

        var response = pois.Select(poi => {
            var ratingData = ratings.GetValueOrDefault(poi.Id);
            double? avgRating = ratingData != null ? Math.Floor(ratingData.Avg * 2.0) / 2.0 : null;
            int? count = ratingData?.Count;

            return new MapPinResponse(
                Id: poi.Id,
                Label: poi.Name,
                Latitude: poi.Latitude,
                Longitude: poi.Longitude,
                Type: poi.Type,
                Description: poi.Description,
                Category: poi.Category,
                ImageUrl: poi.ImageUrl,
                OpeningHours: poi.OpeningHours,
                Fee: poi.Fee,
                Phone: poi.Phone,
                Website: poi.Website,
                Rating: avgRating,
                RatingsCount: count
            );
        }).ToList();

        _logger.LogInformation("[MapController] Returning {Count} POIs to client", response.Count);
        return Ok(response);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<MapPinResponse>>> SearchPois(
        [FromQuery] string query,
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] double radius = 10.0)
    {
        _logger.LogInformation("[MapController] GET /api/map/search?query={Query}&lat={Lat}&lon={Lon}&radius={Radius}km",
            query, lat, lon, radius);

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return BadRequest("Query must be at least 2 characters.");

        if (radius is <= 0 or > 50)
            return BadRequest("Radius must be between 0 and 50 km.");

        var pois = await _overpassService.SearchPoisAsync(query, lat, lon, radius);

        var poiIds = pois.Select(p => p.Id).ToList();
        var ratings = await _dbContext.PoiRatings
            .Where(r => poiIds.Contains(r.PoiId))
            .GroupBy(r => r.PoiId)
            .Select(g => new { PoiId = g.Key, Avg = g.Average(r => (double)r.Score), Count = g.Count() })
            .ToDictionaryAsync(x => x.PoiId, x => x);

        var response = pois.Select(poi => {
            var ratingData = ratings.GetValueOrDefault(poi.Id);
            double? avgRating = ratingData != null ? Math.Floor(ratingData.Avg * 2.0) / 2.0 : null;
            int? count = ratingData?.Count;
            
            return new MapPinResponse(
                Id: poi.Id,
                Label: poi.Name,
                Latitude: poi.Latitude,
                Longitude: poi.Longitude,
                Type: poi.Type,
                Description: poi.Description,
                Category: poi.Category,
                ImageUrl: poi.ImageUrl,
                OpeningHours: poi.OpeningHours,
                Fee: poi.Fee,
                Phone: poi.Phone,
                Website: poi.Website,
                Rating: avgRating,
                RatingsCount: count
            );
        }).ToList();

        _logger.LogInformation("[MapController] Search '{Query}' → returning {Count} POIs", query, response.Count);
        return Ok(response);
    }

    [HttpPost("pois/{poiId}/rate")]
    [Authorize]
    public async Task<IActionResult> RatePoi(string poiId, [FromBody] RatePoiRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (request.Score < 0 || request.Score > 10) 
            return BadRequest("Score must be between 0 and 10.");

        var existing = await _dbContext.PoiRatings.FirstOrDefaultAsync(r => r.PoiId == poiId && r.UserId == userId);
        if (existing != null)
        {
            return BadRequest("Hai già votato questa tappa.");
        }
        else
        {
            _dbContext.PoiRatings.Add(new PoiRating
            {
                Id = Guid.NewGuid(),
                PoiId = poiId,
                UserId = userId,
                Score = request.Score,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _dbContext.SaveChangesAsync();
        return Ok();
    }
}
