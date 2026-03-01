using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Xplore.Domain.Entities;
using Xplore.Infrastructure.Identity;

namespace Xplore.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Qui registriamo le tabelle
    public DbSet<Museum> Museums { get; set; }
    public DbSet<ChatInteraction> ChatInteractions { get; set; }
    public DbSet<RadiusLoadingMetric> RadiusLoadingMetrics { get; set; }
    public DbSet<PoiRating> PoiRatings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // IMPORTANTE: Chiamare base per configurare le tabelle Identity
        base.OnModelCreating(modelBuilder);

        // Configurazione Museum
        modelBuilder.Entity<Museum>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        // ChatInteraction configuration
        modelBuilder.Entity<ChatInteraction>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserQuestion).IsRequired();
            entity.Property(e => e.AiResponse).IsRequired();
            entity.HasIndex(e => e.MuseumId); // For multi-tenancy queries
            entity.HasIndex(e => e.SessionId); // For chat history lookups
            entity.HasIndex(e => e.CreatedAt); // For analytics time-based queries
        });

        // ApplicationUser configuration
        modelBuilder.Entity<ApplicationUser>(entity => {
            entity.Property(e => e.DisplayName).HasMaxLength(100);
        });

        // RadiusLoadingMetric configuration
        modelBuilder.Entity<RadiusLoadingMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RadiusKm);   // For GROUP BY queries
            entity.HasIndex(e => e.RecordedAt); // For time-based filtering
        });

        // PoiRating configuration
        modelBuilder.Entity<PoiRating>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PoiId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450); // Matches max len of Identity UserId
            entity.HasIndex(e => e.PoiId);
        });
    }
}