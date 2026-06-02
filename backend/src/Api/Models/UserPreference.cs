namespace Api.Models;

/// <summary>
/// Entity representing a user's display and notification preferences.
/// </summary>
public class UserPreference
{
    public int UserPreferenceId { get; set; }
    public int UserId { get; set; }
    public string ThemePreference { get; set; } = "light";
    public bool EmailNotificationIndicator { get; set; } = true;
    public bool PushNotificationIndicator { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
