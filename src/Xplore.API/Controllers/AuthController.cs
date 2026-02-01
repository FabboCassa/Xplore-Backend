namespace Xplore.API.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Xplore.Infrastructure.Identity;

/// <summary>
/// Authentication controller for user registration, login, and token management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Register a new Personal account (visitor/user).
    /// </summary>
    [HttpPost("register/personal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterPersonal([FromBody] RegisterPersonalRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            AccountType = AccountType.Personal,
            IsPremium = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponse(false, "Registration failed", result.Errors.Select(e => e.Description).ToList()));
        }

        await _userManager.AddToRoleAsync(user, "Visitor");

        _logger.LogInformation("Personal user {Email} registered successfully", request.Email);
        return Ok(new AuthResponse(true, "User registered successfully"));
    }

    /// <summary>
    /// Register a new Business account (museum).
    /// </summary>
    [HttpPost("register/business")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterBusiness([FromBody] RegisterBusinessRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            AccountType = AccountType.Business,
            MuseumId = request.MuseumId,
            CompanyName = request.CompanyName,
            VatNumber = request.VatNumber,
            IsPremium = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponse(false, "Registration failed", result.Errors.Select(e => e.Description).ToList()));
        }

        await _userManager.AddToRoleAsync(user, "MuseumManager");

        _logger.LogInformation("Business user {Email} registered for museum {MuseumId}", request.Email, request.MuseumId);
        return Ok(new AuthResponse(true, "Business account registered successfully"));
    }

    /// <summary>
    /// Login and get JWT tokens.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new AuthResponse(false, "Invalid credentials"));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(new AuthResponse(false, "Invalid credentials"));
        }

        var tokens = await GenerateTokens(user);
        
        // Save refresh token
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Email} ({AccountType}) logged in", request.Email, user.AccountType);
        return Ok(tokens);
    }

    /// <summary>
    /// Get a new access token using a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            return Unauthorized(new AuthResponse(false, "Invalid token"));
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized(new AuthResponse(false, "Invalid token"));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || 
            user.RefreshToken != request.RefreshToken || 
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized(new AuthResponse(false, "Invalid refresh token"));
        }

        var tokens = await GenerateTokens(user);
        
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return Ok(tokens);
    }

    /// <summary>
    /// Guest login - creates a temporary anonymous Personal user.
    /// </summary>
    [HttpPost("guest")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GuestLogin([FromQuery] Guid? museumId = null)
    {
        var guestId = Guid.NewGuid().ToString("N")[..8];
        var user = new ApplicationUser
        {
            UserName = $"guest_{guestId}",
            Email = $"guest_{guestId}@xplore.local",
            DisplayName = $"Guest {guestId}",
            AccountType = AccountType.Personal,
            MuseumId = museumId, // Which museum they're visiting (for context)
            IsPremium = false
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(new AuthResponse(false, "Could not create guest account"));
        }

        await _userManager.AddToRoleAsync(user, "Visitor");

        var tokens = await GenerateTokens(user);
        
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddHours(24); // Guest tokens expire sooner
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Guest user {UserId} created for museum {MuseumId}", user.Id, museumId);
        return Ok(tokens);
    }

    /// <summary>
    /// Get current user info.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserInfo(
            user.Id,
            user.Email!,
            user.DisplayName,
            user.AccountType.ToString(),
            user.IsPremium,
            user.MuseumId,
            user.CompanyName,
            roles.ToList()
        ));
    }

    private async Task<TokenResponse> GenerateTokens(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email!),
            new("account_type", user.AccountType.ToString()),
            new("is_premium", user.IsPremium.ToString().ToLower()),
            new("museum_id", user.MuseumId?.ToString() ?? "")
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(15);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new TokenResponse(accessToken, refreshToken, expiry);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "");
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = false // Allow expired tokens for refresh
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            return tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
        }
        catch
        {
            return null;
        }
    }
}

// DTOs
public record RegisterPersonalRequest(string Email, string Password, string? DisplayName = null);
public record RegisterBusinessRequest(string Email, string Password, Guid MuseumId, string CompanyName, string? DisplayName = null, string? VatNumber = null);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);
public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record AuthResponse(bool Success, string Message, List<string>? Errors = null);
public record UserInfo(string Id, string Email, string? DisplayName, string AccountType, bool IsPremium, Guid? MuseumId, string? CompanyName, List<string> Roles);
