using Microsoft.AspNetCore.Mvc;
using Xplore.Contracts.Map;
using Xplore.Infrastructure.Map;

namespace Xplore.API.Controllers;

/// <summary>
/// Stateless POI proxy controller.
/// </summary>
[ApiController]
[Route("api/map")]
public class MapController : ControllerBase
{
    private readonly OverpassApiService _overpassService;
    private readonly ILogger<MapController> _logger;

    public MapController(OverpassApiService overpassService, ILogger<MapController> logger)
    {
        _overpassService = overpassService;
        _logger = logger;
    }

    [HttpGet("pois")]
    public async Task<ActionResult<List<MapPinResponse>>> GetPois(
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] double radius = 3.0)
    {
        _logger.LogInformation("📍 [MapController] GET /api/map/pois?lat={Lat}&lon={Lon}&radius={Radius}km", lat, lon, radius);

        if (radius is <= 0 or > 50)
            return BadRequest("Radius must be between 0 and 50 km.");

        var pois = await _overpassService.GetPoisAsync(lat, lon, radius);

        var response = pois.Select(poi => new MapPinResponse(
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
            Website: poi.Website
        )).ToList();

        _logger.LogInformation("📍 [MapController] Returning {Count} POIs to client", response.Count);
        return Ok(response);
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<MapPinResponse>>> SearchPois(
        [FromQuery] string query,
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] double radius = 10.0)
    {
        _logger.LogInformation("🔎 [MapController] GET /api/map/search?query={Query}&lat={Lat}&lon={Lon}&radius={Radius}km",
            query, lat, lon, radius);

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return BadRequest("Query must be at least 2 characters.");

        if (radius is <= 0 or > 50)
            return BadRequest("Radius must be between 0 and 50 km.");

        var pois = await _overpassService.SearchPoisAsync(query, lat, lon, radius);

        var response = pois.Select(poi => new MapPinResponse(
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
            Website: poi.Website
        )).ToList();

        _logger.LogInformation("🔎 [MapController] Search '{Query}' → returning {Count} POIs", query, response.Count);
        return Ok(response);
    }
}
