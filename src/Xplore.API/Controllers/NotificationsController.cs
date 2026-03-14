using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xplore.Contracts.Notifications;
using Xplore.Domain.Entities;
using Xplore.Infrastructure.Persistence;

namespace Xplore.API.Controllers;

/// <summary>
/// Controller for push notification device token management.
/// </summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        ApplicationDbContext dbContext,
        ILogger<NotificationsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Register or refresh an FCM device token for the current user.
    /// Upserts: if the (UserId, Token) pair exists, updates the timestamp.
    /// </summary>
    [HttpPost("device-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterDeviceToken([FromBody] RegisterDeviceTokenRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest("Token is required.");

        if (request.Platform is not ("android" or "ios"))
            return BadRequest("Platform must be 'android' or 'ios'.");

        var existing = await _dbContext.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == request.Token);

        if (existing != null)
        {
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            _dbContext.UserDeviceTokens.Add(new UserDeviceToken
            {
                UserId = userId,
                Token = request.Token,
                Platform = request.Platform,
            });
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Device token registered for user {UserId} on {Platform}", userId, request.Platform);
        return NoContent();
    }

    /// <summary>
    /// Remove a device token (e.g. on logout).
    /// </summary>
    [HttpDelete("device-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnregisterDeviceToken([FromQuery] string token)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var existing = await _dbContext.UserDeviceTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == token);

        if (existing != null)
        {
            _dbContext.UserDeviceTokens.Remove(existing);
            await _dbContext.SaveChangesAsync();
        }

        return NoContent();
    }
}
