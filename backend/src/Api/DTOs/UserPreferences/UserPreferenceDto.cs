using System.Text.Json.Serialization;

namespace Api.DTOs.UserPreferences;

/// <summary>
/// Response DTO representing a user's display and notification preferences.
/// </summary>
public class UserPreferenceDto
{
    /// <summary>
    /// The unique identifier for this preference record.
    /// </summary>
    [JsonPropertyName("userPreferenceId")]
    public int UserPreferenceId { get; set; }

    /// <summary>
    /// The identifier of the user who owns these preferences.
    /// </summary>
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    /// <summary>
    /// The user's selected display theme. Valid values: "light", "dark".
    /// </summary>
    [JsonPropertyName("themePreference")]
    public string ThemePreference { get; set; } = "light";

    /// <summary>
    /// Indicates whether the user has email notifications enabled.
    /// </summary>
    [JsonPropertyName("emailNotificationIndicator")]
    public bool EmailNotificationIndicator { get; set; }

    /// <summary>
    /// Indicates whether the user has push notifications enabled.
    /// </summary>
    [JsonPropertyName("pushNotificationIndicator")]
    public bool PushNotificationIndicator { get; set; }

    /// <summary>
    /// The date and time when preferences were first created.
    /// </summary>
    [JsonPropertyName("createdDate")]
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The date and time when preferences were last updated.
    /// </summary>
    [JsonPropertyName("updatedDate")]
    public DateTime UpdatedDate { get; set; }
}
