using Api.DTOs.UserPreferences;
using Api.Models;
using Api.Repositories;
using Api.Services;
using Moq;
using Xunit;

namespace Api.Tests.Services;

/// <summary>
/// Unit tests for UserPreferenceService using Moq for repository mocking.
/// </summary>
public sealed class UserPreferenceServiceTests
{
    private readonly Mock<IUserPreferenceRepository> _mockRepository;
    private readonly UserPreferenceService _service;

    public UserPreferenceServiceTests()
    {
        _mockRepository = new Mock<IUserPreferenceRepository>();
        _service = new UserPreferenceService(_mockRepository.Object);
    }

    #region GetByUserIdAsync Tests

    [Fact]
    public async Task GetByUserIdAsync_WhenExists_ReturnsEnvelopedDto()
    {
        // Arrange
        var entity = Utils.TestDataBuilders.BuildUserPreference(userId: 1, theme: "dark");
        _mockRepository.Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByUserIdAsync(1, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("dark", result.Item.ThemePreference);
        Assert.Equal(1, result.Item.UserId);
        Assert.False(string.IsNullOrWhiteSpace(result.Metadata.TransactionId));
        Assert.Contains("/v1/userpreferences/1", result.Links.Self);
    }

    [Fact]
    public async Task GetByUserIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByUserIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference?)null);

        // Act
        var result = await _service.GetByUserIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WhenExists_UpdatesAndReturnsDto()
    {
        // Arrange
        var existing = Utils.TestDataBuilders.BuildUserPreference(userId: 1, theme: "light");
        _mockRepository.Setup(r => r.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<UserPreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference e, CancellationToken _) => e);

        var request = new UpdateUserPreferenceRequestDto
        {
            ThemePreference = "dark",
            EmailNotificationIndicator = false,
            PushNotificationIndicator = true
        };

        // Act
        var result = await _service.UpdateAsync(1, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("dark", result.Item.ThemePreference);
        Assert.False(result.Item.EmailNotificationIndicator);
        Assert.True(result.Item.PushNotificationIndicator);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<UserPreference>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotExists_CreatesAndReturnsDto()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByUserIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference?)null);
        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<UserPreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreference e, CancellationToken _) => e);

        var request = new UpdateUserPreferenceRequestDto
        {
            ThemePreference = "dark",
            EmailNotificationIndicator = true,
            PushNotificationIndicator = false
        };

        // Act
        var result = await _service.UpdateAsync(2, request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("dark", result.Item.ThemePreference);
        Assert.Equal(2, result.Item.UserId);
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<UserPreference>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
