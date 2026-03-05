using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Xplore.Contracts.Community;
using Xplore.Domain.Entities;
using Xplore.Infrastructure.Identity;
using Xplore.Infrastructure.Persistence;

namespace Xplore.API.Controllers;

/// <summary>
/// Controller for community features: groups and explorer leaderboard.
/// </summary>
[ApiController]
[Authorize]
[Route("api/community")]
public class CommunityController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly ILogger<CommunityController> _logger;

    public CommunityController(
        ApplicationDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher,
        ILogger<CommunityController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    // ── Groups ──

    /// <summary>
    /// Create a new community group. The caller becomes the Admin.
    /// </summary>
    [HttpPost("groups")]
    [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var accessType = (GroupAccessType)request.AccessType;

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            AccessType = accessType,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        // Hash password if password-protected
        if (accessType == GroupAccessType.Password && !string.IsNullOrEmpty(request.Password))
        {
            group.PasswordHash = _passwordHasher.HashPassword(null!, request.Password);
        }

        var membership = new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = userId,
            Role = GroupRole.Admin,
            JoinedAt = DateTime.UtcNow
        };

        _dbContext.Groups.Add(group);
        _dbContext.GroupMembers.Add(membership);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created group {GroupId} '{GroupName}'", userId, group.Id, group.Name);

        return CreatedAtAction(nameof(GetGroupById), new { id = group.Id },
            MapToResponse(group, 1));
    }

    /// <summary>
    /// Get a single group by its ID.
    /// </summary>
    [HttpGet("groups/{id:guid}")]
    [ProducesResponseType(typeof(GroupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupById(Guid id)
    {
        var group = await _dbContext.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null) return NotFound();

        return Ok(MapToResponse(group, group.Members.Count));
    }

    /// <summary>
    /// Join an existing group. Validates password for protected groups.
    /// </summary>
    [HttpPost("groups/{id:guid}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> JoinGroup(Guid id, [FromBody] JoinGroupRequest? request = null)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null) return NotFound();

        // InviteOnly groups cannot be joined via this endpoint
        if (group.AccessType == GroupAccessType.InviteOnly)
            return Forbid();

        // Validate password for protected groups
        if (group.AccessType == GroupAccessType.Password)
        {
            if (string.IsNullOrEmpty(request?.Password) || string.IsNullOrEmpty(group.PasswordHash))
                return StatusCode(403, "Password required to join this group.");

            var result = _passwordHasher.VerifyHashedPassword(null!, group.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
                return StatusCode(403, "Invalid password.");
        }

        var alreadyMember = await _dbContext.GroupMembers
            .AnyAsync(m => m.GroupId == id && m.UserId == userId);

        if (alreadyMember)
            return Conflict("User is already a member of this group.");

        _dbContext.GroupMembers.Add(new GroupMember
        {
            Id = Guid.NewGuid(),
            GroupId = id,
            UserId = userId,
            Role = GroupRole.Member,
            JoinedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("User {UserId} joined group {GroupId}", userId, id);

        return Ok();
    }

    /// <summary>
    /// Leave a group.
    /// </summary>
    [HttpPost("groups/{id:guid}/leave")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveGroup(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var membership = await _dbContext.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == userId);

        if (membership == null) return NotFound("User is not a member of this group.");

        _dbContext.GroupMembers.Remove(membership);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} left group {GroupId}", userId, id);
        return Ok();
    }

    /// <summary>
    /// Get all groups the current user is a member of.
    /// </summary>
    [HttpGet("groups/me")]
    [ProducesResponseType(typeof(List<GroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyGroups()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var groups = await _dbContext.GroupMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Group)
                .ThenInclude(g => g.Members)
            .Select(m => MapToResponse(m.Group, m.Group.Members.Count))
            .ToListAsync();

        return Ok(groups);
    }

    /// <summary>
    /// Browse all public/password groups (paginated). InviteOnly excluded.
    /// </summary>
    [HttpGet("groups")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllGroups(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var groups = await _dbContext.Groups
            .Where(g => g.AccessType != GroupAccessType.InviteOnly)
            .Include(g => g.Members)
            .OrderByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(groups.Select(g => MapToResponse(g, g.Members.Count)));
    }

    /// <summary>
    /// Search groups by name. Excludes InviteOnly groups.
    /// </summary>
    [HttpGet("groups/search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<GroupResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchGroups([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Ok(new List<GroupResponse>());

        var groups = await _dbContext.Groups
            .Where(g => g.AccessType != GroupAccessType.InviteOnly
                        && g.Name.ToLower().Contains(query.ToLower()))
            .Include(g => g.Members)
            .OrderByDescending(g => g.Members.Count)
            .Take(20)
            .ToListAsync();

        return Ok(groups.Select(g => MapToResponse(g, g.Members.Count)));
    }

    // ── Leaderboard ──

    /// <summary>
    /// Get the global explorer leaderboard. Excludes guest users.
    /// </summary>
    [HttpGet("leaderboard")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<LeaderboardEntry>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int top = 50)
    {
        var users = await _dbContext.Users
            .Where(u => u.DisplayName == null
                        || !u.DisplayName.StartsWith("Guest "))
            .OrderByDescending(u => u.TotalScore)
            .ThenBy(u => u.UserName)
            .Take(top)
            .ToListAsync();

        _logger.LogInformation("Leaderboard query returned {Count} users", users.Count);

        var entries = users.Select((u, index) => new LeaderboardEntry(
            u.Id,
            u.DisplayName ?? u.UserName,
            u.VisitedPlacesCount,
            u.GroupVictories,
            u.CommunityContributions,
            u.TotalScore,
            Rank: index + 1
        )).ToList();

        return Ok(entries);
    }

    // ── Helpers ──

    private static GroupResponse MapToResponse(Group g, int memberCount) =>
        new(g.Id, g.Name, g.Description, g.ImageUrl,
            g.CreatedById, g.CreatedAt, memberCount,
            (int)g.AccessType, g.AccessType == GroupAccessType.Password);
}
