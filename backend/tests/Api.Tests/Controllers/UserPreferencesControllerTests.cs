using System.Net;
using System.Net.Http.Json;
using Api.DTOs.Common;
using Api.DTOs.UserPreferences;
using Api.Tests.Integration;
using Xunit;

namespace Api.Tests.Controllers;

/// <summary>
/// Integration tests for UserPreferencesController verifying full HTTP pipeline.
/// </summary>
public sealed class UserPreferencesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _testClient;

    public UserPreferencesControllerTests(CustomWebApplicationFactory applicationFactory)
    {
        _testClient = applicationFactory.CreateClient();
    }

    #region GET /v1/userpreferences/{userId} Tests

    [Fact]
    public async Task GetByUserId_WhenSeededUserExists_ReturnsOkWithEnvelope()
    {
        // Arrange
        var endpointPath = "/v1/userpreferences/1";

        // Act
        var httpResponse = await _testClient.GetAsync(endpointPath);

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var envelope = await httpResponse.Content.ReadFromJsonAsync<ItemResponseDto<UserPreferenceDto>>();
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Item);
        Assert.Equal(1, envelope.Item.UserId);
        Assert.Equal("light", envelope.Item.ThemePreference);
        Assert.True(envelope.Item.EmailNotificationIndicator);
        Assert.True(envelope.Item.PushNotificationIndicator);
    }

    [Fact]
    public async Task GetByUserId_WhenUserNotExists_ReturnsNotFound()
    {
        // Arrange
        var endpointPath = "/v1/userpreferences/999";

        // Act
        var httpResponse = await _testClient.GetAsync(endpointPath);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);

        var error = await httpResponse.Content.ReadFromJsonAsync<ErrorResponseDto>();
        Assert.NotNull(error);
        Assert.Equal("ORG-NTF-001", error.Code);
    }

    [Fact]
    public async Task GetByUserId_WhenCalled_ReturnsMetadataAndLinks()
    {
        // Arrange
        var endpointPath = "/v1/userpreferences/1";

        // Act
        var envelope = await _testClient.GetFromJsonAsync<ItemResponseDto<UserPreferenceDto>>(endpointPath);

        // Assert
        Assert.NotNull(envelope);
        Assert.NotNull(envelope.Metadata);
        Assert.False(string.IsNullOrWhiteSpace(envelope.Metadata.TransactionId));
        Assert.NotNull(envelope.Links);
        Assert.Contains("/v1/userpreferences/1", envelope.Links.Self);
    }

    #endregion

    #region PUT /v1/userpreferences/{userId} Tests

    [Fact]
    public async Task Update_WithValidBody_ReturnsOkWithUpdatedPreferences()
    {
        // Arrange
        var endpointPath = "/v1/userpreferences/1";
        var request = new UpdateUserPreferenceRequestDto
        {
            ThemePreference = "dark",
            EmailNotificationIndicator = false,
            PushNotificationIndicator = true
        };

        // Act
        var httpResponse = await _testClient.PutAsJsonAsync(endpointPath, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

        var envelope = await httpResponse.Content.ReadFromJsonAsync<ItemResponseDto<UserPreferenceDto>>();
        Assert.NotNull(envelope);
        Assert.Equal("dark", envelope.Item.ThemePreference);
        Assert.False(envelope.Item.EmailNotificationIndicator);
        Assert.True(envelope.Item.PushNotificationIndicator);
    }

    [Fact]
    public async Task Update_WithInvalidTheme_ReturnsBadRequest()
    {
        // Arrange
        var endpointPath = "/v1/userpreferences/1";
        var invalidPayload = new { themePreference = "blue", emailNotificationIndicator = true, pushNotificationIndicator = true };

        // Act
        var httpResponse = await _testClient.PutAsJsonAsync(endpointPath, invalidPayload);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
    }

    #endregion
}
