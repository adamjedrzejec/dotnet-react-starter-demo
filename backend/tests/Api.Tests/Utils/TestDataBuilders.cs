namespace Api.Tests.Utils;

/// <summary>
/// Factory methods for creating test data objects.
/// Provides consistent, configurable test fixtures with sensible defaults.
/// </summary>
public static class TestDataBuilders
{
    /// <summary>
    /// Creates a sample health response for testing purposes.
    /// </summary>
    /// <param name="status">Health status value (default: "healthy").</param>
    /// <param name="version">Application version (default: "1.0.0.0").</param>
    /// <param name="environment">Runtime environment (default: "Testing").</param>
    /// <returns>A configured health response DTO.</returns>
    public static Api.DTOs.Health.HealthResponseDto BuildHealthResponse(
        string status = "healthy",
        string version = "1.0.0.0",
        string environment = "Testing")
    {
        return new Api.DTOs.Health.HealthResponseDto
        {
            Status = status,
            Version = version,
            CheckedAtDate = DateTime.UtcNow,
            Environment = environment
        };
    }

    /// <summary>
    /// Creates sample metadata for envelope responses.
    /// </summary>
    /// <param name="transactionId">Correlation identifier (default: new GUID).</param>
    /// <param name="totalCount">Optional total count for collections.</param>
    /// <returns>A configured metadata DTO.</returns>
    public static Api.DTOs.Common.MetadataDto BuildMetadata(
        string? transactionId = null,
        int? totalCount = null)
    {
        return new Api.DTOs.Common.MetadataDto
        {
            Timestamp = DateTime.UtcNow,
            TransactionId = transactionId ?? Guid.NewGuid().ToString("D"),
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Creates sample links for envelope responses.
    /// </summary>
    /// <param name="selfUrl">Canonical URL of the resource.</param>
    /// <param name="nextUrl">Optional next page URL.</param>
    /// <param name="prevUrl">Optional previous page URL.</param>
    /// <returns>A configured links DTO.</returns>
    public static Api.DTOs.Common.LinksDto BuildLinks(
        string selfUrl = "http://localhost/v1/resource",
        string? nextUrl = null,
        string? prevUrl = null)
    {
        return new Api.DTOs.Common.LinksDto
        {
            Self = selfUrl,
            Next = nextUrl,
            Prev = prevUrl
        };
    }

    /// <summary>
    /// Creates a sample user preference entity for testing.
    /// </summary>
    /// <param name="userPreferenceId">Preference record ID (default: 1).</param>
    /// <param name="userId">Owning user ID (default: 1).</param>
    /// <param name="theme">Display theme (default: "light").</param>
    /// <param name="emailNotification">Email notifications enabled (default: true).</param>
    /// <param name="pushNotification">Push notifications enabled (default: true).</param>
    /// <returns>A configured UserPreference entity.</returns>
    public static Api.Models.UserPreference BuildUserPreference(
        int userPreferenceId = 1,
        int userId = 1,
        string theme = "light",
        bool emailNotification = true,
        bool pushNotification = true)
    {
        return new Api.Models.UserPreference
        {
            UserPreferenceId = userPreferenceId,
            UserId = userId,
            ThemePreference = theme,
            EmailNotificationIndicator = emailNotification,
            PushNotificationIndicator = pushNotification,
            CreatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }
}
