namespace Xplore.API.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Xplore.Application.Services;
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
    private readonly HttpClient _httpClient;
    private readonly IEmailSender _emailSender;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        HttpClient httpClient,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _emailSender = emailSender;
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
            UserName = request.UserName,
            Email = request.Email,
            DisplayName = request.UserName,
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
            user = await _userManager.FindByNameAsync(request.Email); // Fallback to checking by username
        }

        if (user == null)
        {
            return Unauthorized(new AuthResponse(false, "Credenziali non valide"));
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Account locked out: {Email}", request.Email);
                return StatusCode(429, new AuthResponse(false, "Account temporaneamente bloccato. Riprova tra 15 minuti."));
            }
            if (result.RequiresTwoFactor)
            {
                return Ok(new { requiresTwoFactor = true, userId = user.Id });
            }
            return Unauthorized(new AuthResponse(false, "Credenziali non valide"));
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

    // ========================
    // External Login (Google / Apple)
    // ========================

    /// <summary>
    /// External login via Google or Apple ID token.
    /// The mobile app obtains an idToken from the provider SDK and sends it here.
    /// </summary>
    [HttpPost("external-login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExternalLogin([FromBody] ExternalLoginRequest request)
    {
        // 1. Validate idToken based on provider
        var (isValid, email, name) = request.Provider switch
        {
            "Google" => await ValidateGoogleToken(request.IdToken),
            "Apple"  => await ValidateAppleToken(request.IdToken),
            _        => (false, (string?)null, (string?)null)
        };

        if (!isValid || email == null)
            return Unauthorized(new AuthResponse(false, "Credenziali non valide"));

        // 2. Find or create user
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = name ?? email.Split('@')[0],
                AccountType = AccountType.Personal,
                EmailConfirmed = true, // OAuth = email already verified
            };
            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                return BadRequest(new AuthResponse(false, "Registrazione fallita"));

            await _userManager.AddToRoleAsync(user, "Visitor");
            _logger.LogInformation("Created new user via {Provider}: {Email}", request.Provider, email);
        }

        // 3. Verify Lockout status
        if (await _userManager.IsLockedOutAsync(user))
        {
            _logger.LogWarning("Account locked out: {Email}", email);
            return StatusCode(429, new AuthResponse(false, "Account temporaneamente bloccato."));
        }

        // 4. Verify 2FA
        if (user.TwoFactorEnabled)
        {
            return Ok(new { requiresTwoFactor = true, userId = user.Id });
        }

        // 5. Generate tokens
        var tokens = await GenerateTokens(user);
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {Email} logged in via {Provider}", email, request.Provider);
        return Ok(tokens);
    }

    // ========================
    // Two-Factor Authentication (2FA)
    // ========================

    /// <summary>
    /// Setup authenticator app (TOTP) for the current user.
    /// Returns the shared key and otpauth URI for QR code generation.
    /// </summary>
    [Authorize]
    [HttpPost("2fa/setup")]
    [ProducesResponseType(typeof(TwoFactorSetupResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Setup2FA()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        if (user.AccountType == AccountType.Personal && user.Email!.EndsWith("@xplore.local"))
            return BadRequest(new AuthResponse(false, "Gli account guest non supportano l'autenticazione a due fattori. Registrati per abilitare la 2FA."));

        var key = await _userManager.GetAuthenticatorKeyAsync(user);

        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var encodedEmail = Uri.EscapeDataString(user.Email!);
        var uri = $"otpauth://totp/Xplore:{encodedEmail}?secret={key}&issuer=Xplore";

        return Ok(new TwoFactorSetupResponse(key!, uri));
    }

    /// <summary>
    /// Verify a TOTP or Email code during login or 2FA setup.
    /// On success, enables 2FA (if not yet) and returns JWT tokens.
    /// </summary>
    [HttpPost("2fa/verify")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Verify2FA([FromBody] TwoFactorVerifyRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return Unauthorized(new AuthResponse(false, "Credenziali non valide"));

        bool isValid = request.Provider switch
        {
            "Authenticator" => await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, request.Code),
            "Email" => await _userManager.VerifyTwoFactorTokenAsync(
                user, TokenOptions.DefaultEmailProvider, request.Code),
            _ => false
        };

        if (!isValid)
            return Unauthorized(new AuthResponse(false, "Codice non valido"));

        // Enable 2FA if not yet enabled (first-time setup)
        if (!await _userManager.GetTwoFactorEnabledAsync(user))
            await _userManager.SetTwoFactorEnabledAsync(user, true);

        var tokens = await GenerateTokens(user);
        user.RefreshToken = tokens.RefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("User {UserId} completed 2FA verification via {Provider}", request.UserId, request.Provider);
        return Ok(tokens);
    }

    /// <summary>
    /// Send a 2FA verification code via email.
    /// </summary>
    [HttpPost("2fa/send-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendEmail2FA([FromBody] SendEmail2FARequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return Ok(new AuthResponse(true, "Se l'utente esiste, il codice è stato inviato.")); // Don't reveal if user exists

        if (user.AccountType == AccountType.Personal && user.Email!.EndsWith("@xplore.local"))
            return BadRequest(new AuthResponse(false, "Gli account guest non supportano l'autenticazione a due fattori."));

        var code = await _userManager.GenerateTwoFactorTokenAsync(user, TokenOptions.DefaultEmailProvider);

        await _emailSender.SendEmailAsync(user.Email!, "Codice di Verifica Xplore",
            $"Il tuo codice di verifica 2FA è: {code}. Inseriscilo nell'app per completare l'accesso. Il codice scadrà a breve.");

        _logger.LogInformation("2FA email code generated for user {UserId} (code: {Code})", request.UserId, code);
        return Ok(new AuthResponse(true, "Codice inviato"));
    }

    // ========================
    // Private Helpers
    // ========================

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

    private async Task<(bool IsValid, string? Email, string? Name)> ValidateGoogleToken(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Auth:Google:ClientId"] }
            });
            return (true, payload.Email, payload.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google token validation failed");
            return (false, null, null);
        }
    }

    private async Task<(bool IsValid, string? Email, string? Name)> ValidateAppleToken(string idToken)
    {
        try
        {
            // Apple's public keys endpoint
            var appleKeysJson = await _httpClient.GetStringAsync("https://appleid.apple.com/auth/keys");
            var appleKeys = new JsonWebKeySet(appleKeysJson);

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(idToken);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = appleKeys.GetSigningKeys(),
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudience = _configuration["Auth:Apple:ClientId"], // e.g. org.xplore.project
                ValidateLifetime = true
            };

            var principal = handler.ValidateToken(idToken, validationParameters, out var validatedToken);
            var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("email")?.Value;
            var name = principal.FindFirst(ClaimTypes.Name)?.Value; // Apple only sends name on first login

            return (true, email, name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Apple token validation failed");
            return (false, null, null);
        }
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
public record RegisterPersonalRequest(string Email, string Password, string UserName);
public record RegisterBusinessRequest(string Email, string Password, Guid MuseumId, string CompanyName, string? DisplayName = null, string? VatNumber = null);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);
public record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
public record AuthResponse(bool Success, string Message, List<string>? Errors = null);
public record UserInfo(string Id, string Email, string? DisplayName, string AccountType, bool IsPremium, Guid? MuseumId, string? CompanyName, List<string> Roles);
public record ExternalLoginRequest(string Provider, string IdToken);
public record TwoFactorSetupResponse(string SharedKey, string QrCodeUri);
public record TwoFactorVerifyRequest(string UserId, string Code, string Provider);
public record SendEmail2FARequest(string UserId);
