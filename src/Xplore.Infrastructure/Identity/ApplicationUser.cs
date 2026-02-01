namespace Xplore.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;

/// <summary>
/// Application user extending ASP.NET Identity.
/// Supports Personal (visitors) and Business (museums) account types.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// User's display name.
    /// </summary>
    public string? DisplayName { get; set; }
    
    /// <summary>
    /// Type of account: Personal (visitor) or Business (museum).
    /// </summary>
    public AccountType AccountType { get; set; } = AccountType.Personal;
    
    /// <summary>
    /// For Personal accounts: whether they have a premium subscription.
    /// </summary>
    public bool IsPremium { get; set; } = false;
    
    /// <summary>
    /// For Business accounts: the museum they manage.
    /// Null for Personal accounts (visitors).
    /// </summary>
    public Guid? MuseumId { get; set; }
    
    /// <summary>
    /// For Business accounts: company/organization name.
    /// </summary>
    public string? CompanyName { get; set; }
    
    /// <summary>
    /// For Business accounts: VAT/Tax ID.
    /// </summary>
    public string? VatNumber { get; set; }
    
    /// <summary>
    /// When the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Refresh token for JWT authentication.
    /// </summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// When the refresh token expires.
    /// </summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }
}

/// <summary>
/// Defines the type of user account.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Regular visitor/user. Can be free or premium.
    /// </summary>
    Personal = 0,
    
    /// <summary>
    /// Business account (Museum). Can upload PDFs, view analytics.
    /// </summary>
    Business = 1
}
