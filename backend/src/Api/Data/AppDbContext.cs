using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

/// <summary>
/// Application database context for Entity Framework Core.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasKey(e => e.UserPreferenceId);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // Seed default preferences for user 1
        modelBuilder.Entity<UserPreference>().HasData(new UserPreference
        {
            UserPreferenceId = 1,
            UserId = 1,
            ThemePreference = "light",
            EmailNotificationIndicator = true,
            PushNotificationIndicator = true,
            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
