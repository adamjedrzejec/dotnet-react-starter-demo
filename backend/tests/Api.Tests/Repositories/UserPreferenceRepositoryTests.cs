using Api.Data;
using Api.Models;
using Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Api.Tests.Repositories;

/// <summary>
/// Unit tests for UserPreferenceRepository using EF Core InMemory provider.
/// </summary>
public sealed class UserPreferenceRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly UserPreferenceRepository _repository;

    public UserPreferenceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _repository = new UserPreferenceRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_WhenExists_ReturnsPreference()
    {
        // Arrange
        var preference = Utils.TestDataBuilders.BuildUserPreference(userId: 5);
        _context.UserPreferences.Add(preference);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(5, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.UserId);
        Assert.Equal("light", result.ThemePreference);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        var nonExistentUserId = 999;

        // Act
        var result = await _repository.GetByUserIdAsync(nonExistentUserId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_PersistsPreference()
    {
        // Arrange
        var preference = new UserPreference
        {
            UserId = 10,
            ThemePreference = "dark",
            EmailNotificationIndicator = false,
            PushNotificationIndicator = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        // Act
        var result = await _repository.CreateAsync(preference, CancellationToken.None);

        // Assert
        Assert.True(result.UserPreferenceId > 0);
        var persisted = await _context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == 10);
        Assert.NotNull(persisted);
        Assert.Equal("dark", persisted.ThemePreference);
        Assert.False(persisted.EmailNotificationIndicator);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesPreference()
    {
        // Arrange
        var preference = Utils.TestDataBuilders.BuildUserPreference(userId: 7, theme: "light");
        _context.UserPreferences.Add(preference);
        await _context.SaveChangesAsync();
        _context.Entry(preference).State = EntityState.Detached;

        preference.ThemePreference = "dark";
        preference.EmailNotificationIndicator = false;

        // Act
        var result = await _repository.UpdateAsync(preference, CancellationToken.None);

        // Assert
        Assert.Equal("dark", result.ThemePreference);
        Assert.False(result.EmailNotificationIndicator);
    }

    #endregion
}
