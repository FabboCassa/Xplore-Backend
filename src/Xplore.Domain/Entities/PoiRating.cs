using System;

namespace Xplore.Domain.Entities;

public class PoiRating
{
    public Guid Id { get; set; }
    public string PoiId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Voto da 0 a 10.
    /// </summary>
    public int Score { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
