using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Data;

public class DogDoorDbContext : DbContext
{
    public DogDoorDbContext(DbContextOptions<DogDoorDbContext> options) : base(options) { }

    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<AnimalPhoto> AnimalPhotos => Set<AnimalPhoto>();
    public DbSet<DoorEvent> DoorEvents => Set<DoorEvent>();
    public DbSet<DoorConfiguration> DoorConfigurations => Set<DoorConfiguration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Animal>(entity =>
        {
            entity.HasIndex(e => e.Name);
            entity.HasMany(e => e.Photos)
                .WithOne(p => p.Animal)
                .HasForeignKey(p => p.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.DoorEvents)
                .WithOne(d => d.Animal)
                .HasForeignKey(d => d.AnimalId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AnimalPhoto>(entity =>
        {
            entity.HasIndex(e => e.AnimalId);
            entity.HasIndex(e => e.PHash);
        });

        modelBuilder.Entity<DoorEvent>(entity =>
        {
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
        });

        // Seed default configuration
        modelBuilder.Entity<DoorConfiguration>().HasData(new DoorConfiguration
        {
            Id = 1,
            IsEnabled = true,
            AutoCloseEnabled = true,
            AutoCloseDelaySeconds = 10,
            MinConfidenceThreshold = 0.7
        });
    }
}
