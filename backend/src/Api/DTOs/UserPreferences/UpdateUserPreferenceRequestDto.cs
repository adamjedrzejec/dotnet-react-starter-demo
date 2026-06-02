using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Api.DTOs.UserPreferences;

/// <summary>
/// Request DTO for updating a user's display and notification preferences.
/// </summary>
public class UpdateUserPreferenceRequestDto
{
    /// <summary>
    /// The desired display theme. Must be "light" or "dark".
    /// </summary>
    [Required]
    [RegularExpression("^(light|dark)$", ErrorMessage = "Theme must be 'light' or 'dark'.")]
    [JsonPropertyName("themePreference")]
    public string ThemePreference { get; set; } = "light";

    /// <summary>
    /// Indicates whether email notifications should be enabled.
    /// </summary>
    [JsonPropertyName("emailNotificationIndicator")]
    public bool EmailNotificationIndicator { get; set; }

    /// <summary>
    /// Indicates whether push notifications should be enabled.
    /// </summary>
    [JsonPropertyName("pushNotificationIndicator")]
    public bool PushNotificationIndicator { get; set; }
}
