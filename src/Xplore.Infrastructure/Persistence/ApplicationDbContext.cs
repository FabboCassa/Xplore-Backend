using Microsoft.EntityFrameworkCore;
using Xplore.Domain.Entities;

namespace Xplore.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Qui registriamo le tabelle
    public DbSet<Museum> Museums { get; set; }
    public DbSet<ChatInteraction> ChatInteractions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurazioni extra (es. chiavi primarie, vincoli)
        base.OnModelCreating(modelBuilder);

        // Esempio: Il nome del museo è obbligatorio e max 200 caratteri
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
    }
}