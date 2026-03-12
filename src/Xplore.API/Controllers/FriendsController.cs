using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xplore.Contracts.Friends;
using Xplore.Domain.Entities;
using Xplore.Infrastructure.Persistence;

namespace Xplore.API.Controllers;

/// <summary>
/// Controller for friend requests and friendships management.
/// </summary>
[ApiController]
[Authorize]
[Route("api/friends")]
public class FriendsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<FriendsController> _logger;

    public FriendsController(ApplicationDbContext dbContext, ILogger<FriendsController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get list of accepted friends for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<FriendResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFriends()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var friendships = await _dbContext.Friendships
            .Where(f => f.Status == FriendshipStatus.Accepted
                        && (f.RequesterId == userId || f.AddresseeId == userId))
            .ToListAsync();

        var friendUserIds = friendships
            .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
            .ToList();

        var users = await _dbContext.Users
            .Where(u => friendUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        var response = friendships.Select(f =>
        {
            var friendId = f.RequesterId == userId ? f.AddresseeId : f.RequesterId;
            var friendUser = users.GetValueOrDefault(friendId);
            return new FriendResponse(
                friendId,
                friendUser?.DisplayName,
                friendUser?.AvatarData != null,
                f.UpdatedAt ?? f.CreatedAt);
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Get incoming pending friend requests for the current user.
    /// </summary>
    [HttpGet("requests")]
    [ProducesResponseType(typeof(List<FriendRequestResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingRequests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var requests = await _dbContext.Friendships
            .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
            .Join(_dbContext.Users,
                  f => f.RequesterId,
                  u => u.Id,
                  (f, u) => new FriendRequestResponse(
                      f.Id,
                      f.RequesterId,
                      u.DisplayName,
                      u.AvatarData != null,
                      f.CreatedAt))
            .ToListAsync();

        return Ok(requests);
    }

    /// <summary>
    /// Send a friend request to another user.
    /// </summary>
    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (userId == request.AddresseeId)
            return BadRequest("Non puoi inviare una richiesta di amicizia a te stesso.");

        var targetUser = await _dbContext.Users.FindAsync(request.AddresseeId);
        if (targetUser == null)
            return NotFound("Utente non trovato.");

        // Check for existing friendship in either direction
        var existing = await _dbContext.Friendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterId == userId && f.AddresseeId == request.AddresseeId)
                || (f.RequesterId == request.AddresseeId && f.AddresseeId == userId));

        if (existing != null)
        {
            if (existing.Status == FriendshipStatus.Accepted)
                return Conflict("Siete già amici.");
            if (existing.Status == FriendshipStatus.Pending)
                return Conflict("Esiste già una richiesta di amicizia pendente.");
            // If rejected, allow re-sending: remove old and create new
            _dbContext.Friendships.Remove(existing);
        }

        var friendship = new Friendship
        {
            Id = Guid.NewGuid(),
            RequesterId = userId,
            AddresseeId = request.AddresseeId,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Friendships.Add(friendship);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} sent friend request to {AddresseeId}", userId, request.AddresseeId);
        return Ok();
    }

    /// <summary>
    /// Accept a pending friend request.
    /// </summary>
    [HttpPost("request/{id:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AcceptRequest(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var friendship = await _dbContext.Friendships.FindAsync(id);
        if (friendship == null) return NotFound("Richiesta non trovata.");

        if (friendship.AddresseeId != userId)
            return StatusCode(403, "Solo il destinatario può accettare la richiesta.");

        if (friendship.Status != FriendshipStatus.Pending)
            return BadRequest("La richiesta non è più pendente.");

        friendship.Status = FriendshipStatus.Accepted;
        friendship.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} accepted friend request {RequestId} from {RequesterId}",
            userId, id, friendship.RequesterId);
        return Ok();
    }

    /// <summary>
    /// Reject a pending friend request or cancel one you sent.
    /// </summary>
    [HttpPost("request/{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectRequest(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var friendship = await _dbContext.Friendships.FindAsync(id);
        if (friendship == null) return NotFound("Richiesta non trovata.");

        // Both the requester (cancel) and addressee (reject) can do this
        if (friendship.AddresseeId != userId && friendship.RequesterId != userId)
            return StatusCode(403, "Non hai i permessi per questa operazione.");

        if (friendship.Status != FriendshipStatus.Pending)
            return BadRequest("La richiesta non è più pendente.");

        _dbContext.Friendships.Remove(friendship);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} rejected/cancelled friend request {RequestId}", userId, id);
        return Ok();
    }

    /// <summary>
    /// Remove an existing friend.
    /// </summary>
    [HttpDelete("{friendUserId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFriend(string friendUserId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var friendship = await _dbContext.Friendships
            .FirstOrDefaultAsync(f =>
                f.Status == FriendshipStatus.Accepted
                && ((f.RequesterId == userId && f.AddresseeId == friendUserId)
                    || (f.RequesterId == friendUserId && f.AddresseeId == userId)));

        if (friendship == null)
            return NotFound("Amicizia non trovata.");

        _dbContext.Friendships.Remove(friendship);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} removed friend {FriendUserId}", userId, friendUserId);
        return Ok();
    }

    /// <summary>
    /// Search users by display name or username. Excludes guests.
    /// Returns friendship status relative to the current user.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<SearchUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers([FromQuery] string query)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Ok(new List<SearchUserResponse>());

        var users = await _dbContext.Users
            .Where(u => u.Id != userId
                        && (u.DisplayName == null || !u.DisplayName.StartsWith("Guest "))
                        && (u.DisplayName != null && u.DisplayName.ToLower().Contains(query.ToLower())
                            || u.UserName != null && u.UserName.ToLower().Contains(query.ToLower())))
            .Take(20)
            .ToListAsync();

        var userIds = users.Select(u => u.Id).ToList();

        // Get all friendships between current user and the found users
        var friendships = await _dbContext.Friendships
            .Where(f => (f.RequesterId == userId && userIds.Contains(f.AddresseeId))
                        || (f.AddresseeId == userId && userIds.Contains(f.RequesterId)))
            .ToListAsync();

        var response = users.Select(u =>
        {
            var friendship = friendships.FirstOrDefault(f =>
                (f.RequesterId == userId && f.AddresseeId == u.Id)
                || (f.RequesterId == u.Id && f.AddresseeId == userId));

            string? status = friendship?.Status switch
            {
                FriendshipStatus.Pending => "pending",
                FriendshipStatus.Accepted => "accepted",
                _ => null
            };

            return new SearchUserResponse(
                u.Id,
                u.DisplayName,
                u.AvatarData != null,
                status);
        }).ToList();

        return Ok(response);
    }
}
