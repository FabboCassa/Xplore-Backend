using System;
using System.Collections.Generic;
using System.Text;

namespace Xplore.Domain.Entities;

public class Museum
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;

    // Utile per il futuro (Audit)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}