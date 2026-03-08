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

        var groupMembers = await _dbContext.GroupMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Group)
                .ThenInclude(g => g.Members)
            .ToListAsync();

        var groups = groupMembers
            .Select(m => MapToResponse(m.Group, m.Group.Members.Count))
            .ToList();

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

    /// <summary>
    /// Get the details of a group, including its members.
    /// </summary>
    [HttpGet("groups/{id:guid}/detail")]
    [ProducesResponseType(typeof(GroupDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGroupDetail(Guid id)
    {
        var group = await _dbContext.Groups
            .FirstOrDefaultAsync(g => g.Id == id);

        if (group == null) return NotFound();

        var members = await _dbContext.GroupMembers
            .Where(m => m.GroupId == id)
            .Join(_dbContext.Users, 
                  m => m.UserId, 
                  u => u.Id, 
                  (m, u) => new GroupMemberResponse(
                      m.UserId,
                      u.DisplayName ?? u.UserName ?? string.Empty,
                      (int)m.Role,
                      m.JoinedAt))
            .ToListAsync();

        var response = new GroupDetailResponse(
            group.Id,
            group.Name,
            group.Description,
            group.ImageUrl,
            group.CreatedById,
            group.CreatedAt,
            members.Count,
            (int)group.AccessType,
            group.PasswordHash != null,
            members
        );

        return Ok(response);
    }

    /// <summary>
    /// Delete a group. Only the admin (creator) can do this.
    /// </summary>
    [HttpDelete("groups/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteGroup(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null) return NotFound();

        if (group.CreatedById != userId)
            return StatusCode(403, "Only the group creator can delete the group.");

        _dbContext.Groups.Remove(group);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} deleted group {GroupId}", userId, id);
        return Ok();
    }

    /// <summary>
    /// Change the visibility and password of a group.
    /// </summary>
    [HttpPut("groups/{id:guid}/visibility")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ChangeGroupVisibility(Guid id, [FromBody] ChangeGroupVisibilityRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var group = await _dbContext.Groups.FindAsync(id);
        if (group == null) return NotFound();

        if (group.CreatedById != userId)
            return StatusCode(403, "Only the group creator can change the visibility.");

        var newAccessType = (GroupAccessType)request.AccessType;
        group.AccessType = newAccessType;

        if (newAccessType == GroupAccessType.Password)
        {
            if (string.IsNullOrEmpty(request.Password))
                return BadRequest("Password is required for password-protected groups.");
                
            group.PasswordHash = _passwordHasher.HashPassword(null!, request.Password);
        }
        else
        {
            group.PasswordHash = null;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} changed visibility of group {GroupId} to {AccessType}", userId, id, newAccessType);
        return Ok();
    }

    // ── Visits ──

    /// <summary>
    /// Record a place as visited by the current user.
    /// </summary>
    [HttpPost("visits")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VisitPlace([FromBody] VisitPlaceRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var existingVisit = await _dbContext.VisitedPlaces
            .FirstOrDefaultAsync(vp => vp.UserId == userId && vp.PlaceId == request.PlaceId);

        if (existingVisit != null)
            return Ok(); // Already visited

        var visit = new VisitedPlace
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PlaceId = request.PlaceId,
            VisitedAt = DateTime.UtcNow
        };

        _dbContext.VisitedPlaces.Add(visit);

        // Update user's count
        var user = await _dbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.VisitedPlacesCount++;
        }

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} visited place {PlaceId}", userId, request.PlaceId);
        return Ok();
    }

    // ── Competitions ──

    /// <summary>
    /// Create a new competition for a group. Only the admin can do this.
    /// </summary>
    [HttpPost("groups/{groupId:guid}/competitions")]
    [ProducesResponseType(typeof(CompetitionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCompetition(Guid groupId, [FromBody] CreateCompetitionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var group = await _dbContext.Groups.FindAsync(groupId);
        if (group == null) return NotFound("Group not found.");

        if (group.CreatedById != userId)
            return StatusCode(403, "Only the group creator can create competitions.");

        var competition = new Competition
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            Name = request.Name,
            Type = (CompetitionType)request.Type,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Rules = request.Rules.Select(r => new CompetitionRule
            {
                Id = Guid.NewGuid(),
                ActionType = (CompetitionActionType)r.ActionType,
                PointsAwarded = r.PointsAwarded,
                TargetPlaceId = r.TargetPlaceId
            }).ToList()
        };

        _dbContext.Competitions.Add(competition);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} created competition {CompId} in group {GroupId}", userId, competition.Id, groupId);

        var response = new CompetitionResponse(
            competition.Id, competition.GroupId, competition.Name, (int)competition.Type,
            competition.StartDate, competition.EndDate, competition.IsActive, competition.CreatedAt,
            competition.Rules.Select(r => new CompetitionRuleResponse(r.Id, (int)r.ActionType, r.PointsAwarded, r.TargetPlaceId)).ToList()
        );

        return CreatedAtAction(nameof(GetCompetitions), new { groupId = groupId }, response);
    }

    /// <summary>
    /// Edit an existing competition. Only the group admin can do this.
    /// </summary>
    [HttpPut("groups/{groupId:guid}/competitions/{compId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateCompetition(Guid groupId, Guid compId, [FromBody] CreateCompetitionRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var group = await _dbContext.Groups.FindAsync(groupId);
        if (group == null) return NotFound("Group not found.");

        if (group.CreatedById != userId)
            return StatusCode(403, "Only the group creator can update competitions.");

        var competition = await _dbContext.Competitions
            .Include(c => c.Rules)
            .FirstOrDefaultAsync(c => c.Id == compId && c.GroupId == groupId);

        if (competition == null) return NotFound("Competition not found.");

        competition.Name = request.Name;
        competition.Type = (CompetitionType)request.Type;
        competition.StartDate = request.StartDate;
        competition.EndDate = request.EndDate;

        // Replace rules
        _dbContext.CompetitionRules.RemoveRange(competition.Rules);
        competition.Rules = request.Rules.Select(r => new CompetitionRule
        {
            Id = Guid.NewGuid(),
            CompetitionId = compId,
            ActionType = (CompetitionActionType)r.ActionType,
            PointsAwarded = r.PointsAwarded,
            TargetPlaceId = r.TargetPlaceId
        }).ToList();

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated competition {CompId}", userId, compId);
        return Ok();
    }

    /// <summary>
    /// Get all competitions for a group.
    /// </summary>
    [HttpGet("groups/{groupId:guid}/competitions")]
    [ProducesResponseType(typeof(List<CompetitionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompetitions(Guid groupId)
    {
        var competitions = await _dbContext.Competitions
            .Where(c => c.GroupId == groupId)
            .Include(c => c.Rules)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var responses = competitions.Select(c => new CompetitionResponse(
            c.Id, c.GroupId, c.Name, (int)c.Type,
            c.StartDate, c.EndDate, c.IsActive, c.CreatedAt,
            c.Rules.Select(r => new CompetitionRuleResponse(r.Id, (int)r.ActionType, r.PointsAwarded, r.TargetPlaceId)).ToList()
        )).ToList();

        return Ok(responses);
    }

    /// <summary>
    /// Get the leaderboard for a specific competition.
    /// Calculates points based on rules and visits on the fly.
    /// </summary>
    [HttpGet("groups/{groupId:guid}/competitions/{compId:guid}/leaderboard")]
    [ProducesResponseType(typeof(List<CompetitionLeaderboardEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompetitionLeaderboard(Guid groupId, Guid compId)
    {
        var competition = await _dbContext.Competitions
            .Include(c => c.Rules)
            .FirstOrDefaultAsync(c => c.Id == compId && c.GroupId == groupId);

        if (competition == null) return NotFound("Competition not found.");

        var members = await _dbContext.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Select(m => m.UserId)
            .ToListAsync();

        if (!members.Any()) return Ok(new List<CompetitionLeaderboardEntryResponse>());

        var query = _dbContext.VisitedPlaces.Where(vp => members.Contains(vp.UserId));

        if (competition.StartDate.HasValue)
            query = query.Where(vp => vp.VisitedAt >= competition.StartDate.Value);
        if (competition.EndDate.HasValue)
            query = query.Where(vp => vp.VisitedAt <= competition.EndDate.Value);

        var visits = await query.ToListAsync();

        var userPoints = members.ToDictionary(m => m, m => 0);

        foreach (var rule in competition.Rules)
        {
            if (rule.ActionType == CompetitionActionType.VisitPlace)
            {
                foreach (var visit in visits)
                {
                    if (string.IsNullOrEmpty(rule.TargetPlaceId) || rule.TargetPlaceId == visit.PlaceId)
                    {
                        userPoints[visit.UserId] += rule.PointsAwarded;
                    }
                }
            }
        }

        var usersInfo = await _dbContext.Users
            .Where(u => members.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName ?? u.UserName);

        var leaderboard = userPoints
            .Select(kp => new { UserId = kp.Key, Points = kp.Value, Name = usersInfo.GetValueOrDefault(kp.Key) })
            .OrderByDescending(x => x.Points)
            .ThenBy(x => x.Name)
            .Select((x, i) => new CompetitionLeaderboardEntryResponse(x.UserId, x.Name, x.Points, i + 1))
            .ToList();

        return Ok(leaderboard);
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
