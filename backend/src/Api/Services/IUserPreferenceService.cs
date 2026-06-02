using Api.DTOs.Common;
using Api.DTOs.UserPreferences;

namespace Api.Services;

/// <summary>
/// Defines business logic operations for user preferences.
/// </summary>
public interface IUserPreferenceService
{
    /// <summary>
    /// Retrieves a user's preferences wrapped in an envelope response.
    /// </summary>
    Task<ItemResponseDto<UserPreferenceDto>?> GetByUserIdAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Updates or creates a user's preferences (upsert) and returns the envelope response.
    /// </summary>
    Task<ItemResponseDto<UserPreferenceDto>> UpdateAsync(int userId, UpdateUserPreferenceRequestDto request, CancellationToken cancellationToken);
}
