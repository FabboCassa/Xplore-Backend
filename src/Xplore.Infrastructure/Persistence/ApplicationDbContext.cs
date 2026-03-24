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

    public DbSet<Museum> Museums { get; set; }
    public DbSet<ChatInteraction> ChatInteractions { get; set; }
    public DbSet<RadiusLoadingMetric> RadiusLoadingMetrics { get; set; }
    public DbSet<PoiRating> PoiRatings { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<VisitedPlace> VisitedPlaces { get; set; }
    public DbSet<Competition> Competitions { get; set; }
    public DbSet<CompetitionRule> CompetitionRules { get; set; }
    public DbSet<Friendship> Friendships { get; set; }
    public DbSet<UserDeviceToken> UserDeviceTokens { get; set; }
    public DbSet<GroupInvite> GroupInvites { get; set; }
    public DbSet<SavedRoute> SavedRoutes { get; set; }
    public DbSet<SavedRouteWaypoint> SavedRouteWaypoints { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Museum configuration
        modelBuilder.Entity<Museum>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
        });

        // ChatInteraction configuration
        modelBuilder.Entity<ChatInteraction>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserQuestion).IsRequired();
            entity.Property(e => e.AiResponse).IsRequired();
            entity.HasIndex(e => e.MuseumId);
            entity.HasIndex(e => e.SessionId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ApplicationUser configuration
        modelBuilder.Entity<ApplicationUser>(entity => {
            entity.Property(e => e.DisplayName).HasMaxLength(100);
        });

        // RadiusLoadingMetric configuration
        modelBuilder.Entity<RadiusLoadingMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.RadiusKm);
            entity.HasIndex(e => e.RecordedAt);
        });

        // PoiRating configuration
        modelBuilder.Entity<PoiRating>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PoiId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => e.PoiId);
        });

        // Group configuration
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.PasswordHash).HasMaxLength(500);
            entity.Property(e => e.CreatedById).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => e.CreatedById);
            entity.HasIndex(e => e.AccessType);
            entity.HasMany(e => e.Members)
                  .WithOne(e => e.Group)
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // GroupMember configuration
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => new { e.GroupId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        // VisitedPlace configuration
        modelBuilder.Entity<VisitedPlace>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.PlaceId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.PlaceId }).IsUnique(); // Prevent duplicate visits to same place by same user if desired, or at least index it
        });

        // Competition configuration
        modelBuilder.Entity<Competition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.GroupId);
            entity.HasMany(e => e.Rules)
                  .WithOne(e => e.Competition)
                  .HasForeignKey(e => e.CompetitionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CompetitionRule configuration
        modelBuilder.Entity<CompetitionRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TargetPlaceId).HasMaxLength(100);
        });

        // Friendship configuration
        modelBuilder.Entity<Friendship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequesterId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.AddresseeId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => new { e.RequesterId, e.AddresseeId }).IsUnique();
            entity.HasIndex(e => e.AddresseeId);
            entity.HasIndex(e => e.Status);
        });

        // UserDeviceToken configuration
        modelBuilder.Entity<UserDeviceToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Platform).IsRequired().HasMaxLength(10);
            entity.HasIndex(e => new { e.UserId, e.Token }).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        // SavedRoute configuration
        modelBuilder.Entity<SavedRoute>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ShareToken).IsRequired().HasMaxLength(64);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ShareToken).IsUnique();
            entity.HasMany(e => e.Waypoints)
                  .WithOne(e => e.SavedRoute)
                  .HasForeignKey(e => e.SavedRouteId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SavedRouteWaypoint configuration
        modelBuilder.Entity<SavedRouteWaypoint>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PlaceId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.SavedRouteId);
        });

        // GroupInvite configuration
        modelBuilder.Entity<GroupInvite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InviterId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.InviteeId).IsRequired().HasMaxLength(450);
            entity.HasIndex(e => new { e.GroupId, e.InviteeId })
                  .HasFilter("\"Status\" = 0")
                  .IsUnique();
            entity.HasIndex(e => e.InviteeId);
            entity.HasOne(e => e.Group)
                  .WithMany()
                  .HasForeignKey(e => e.GroupId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}