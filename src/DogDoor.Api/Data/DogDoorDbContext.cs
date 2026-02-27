using DogDoor.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DogDoor.Api.Data;

public class DogDoorDbContext : DbContext
{
    public DogDoorDbContext(DbContextOptions<DogDoorDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserGuest> UserGuests => Set<UserGuest>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Animal> Animals => Set<Animal>();
    public DbSet<AnimalPhoto> AnimalPhotos => Set<AnimalPhoto>();
    public DbSet<DoorEvent> DoorEvents => Set<DoorEvent>();
    public DbSet<DoorConfiguration> DoorConfigurations => Set<DoorConfiguration>();
    public DbSet<NotificationPreferences> NotificationPreferences => Set<NotificationPreferences>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<UserGuest>(entity =>
        {
            entity.HasKey(e => new { e.OwnerId, e.GuestId });

            entity.HasOne(e => e.Owner)
                .WithMany(u => u.OwnedGuests)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Guest)
                .WithMany(u => u.GuestOf)
                .HasForeignKey(e => e.GuestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Invitation>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.InvitedBy)
                .WithMany(u => u.SentInvitations)
                .HasForeignKey(e => e.InvitedById)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.TokenPrefix);

            entity.HasOne(e => e.User)
                .WithMany(u => u.PasswordResetTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Animal>(entity =>
        {
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Name);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Animals)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Direction);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DoorConfiguration>(entity =>
        {
            entity.HasOne(e => e.User)
                .WithMany(u => u.DoorConfigurations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationPreferences>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(u => u.NotificationPreferences)
                .HasForeignKey<NotificationPreferences>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.ExternalLogins)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
