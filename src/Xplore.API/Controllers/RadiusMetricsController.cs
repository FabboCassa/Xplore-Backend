using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xplore.Contracts.Map;
using Xplore.Domain.Entities;
using Xplore.Infrastructure.Persistence;

namespace Xplore.API.Controllers;

/// <summary>
/// Technical metrics controller for tracking POI loading times per radius step.
/// Used by the mobile app to submit measurements and retrieve averages.
/// </summary>
[ApiController]
[Route("api/metrics")]
public class RadiusMetricsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RadiusMetricsController> _logger;

    public RadiusMetricsController(ApplicationDbContext db, ILogger<RadiusMetricsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Records a loading time measurement for a given radius.
    /// Called by the mobile app after each POI fetch completes.
    /// </summary>
    [HttpPost("loading")]
    public async Task<IActionResult> PostLoadingTime([FromBody] RadiusLoadingRequest request)
    {
        if (request.RadiusKm is <= 0 or > 50)
            return BadRequest("RadiusKm must be between 0 and 50.");

        if (request.LoadingTimeMs < 0)
            return BadRequest("LoadingTimeMs must be non-negative.");

        var metric = new RadiusLoadingMetric
        {
            Id = Guid.NewGuid(),
            RadiusKm = request.RadiusKm,
            LoadingTimeMs = request.LoadingTimeMs,
            RecordedAt = DateTime.UtcNow,
        };

        _db.RadiusLoadingMetrics.Add(metric);
        await _db.SaveChangesAsync();

        _logger.LogInformation("📊 [Metrics] Recorded {Ms}ms for radius {Radius}km",
            request.LoadingTimeMs, request.RadiusKm);

        return Ok();
    }

    /// <summary>
    /// Returns the average loading time (in ms) for each radius step,
    /// computed over the last 30 days of measurements.
    /// </summary>
    [HttpGet("loading/averages")]
    public async Task<ActionResult<List<RadiusLoadingAverage>>> GetLoadingAverages()
    {
        var cutoff = DateTime.UtcNow.AddDays(-30);

        // Step 1: let SQL compute the AVG (returns double) — fully translatable
        var rawAverages = await _db.RadiusLoadingMetrics
            .Where(m => m.RecordedAt >= cutoff)
            .GroupBy(m => m.RadiusKm)
            .Select(g => new
            {
                RadiusKm = g.Key,
                AvgMs = g.Average(m => (double)m.LoadingTimeMs),
            })
            .OrderBy(a => a.RadiusKm)
            .ToListAsync();

        // Step 2: project to DTO client-side (cast double → long)
        var averages = rawAverages
            .Select(a => new RadiusLoadingAverage(a.RadiusKm, (long)a.AvgMs))
            .ToList();

        return Ok(averages);
    }
}
