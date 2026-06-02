using Api.Models;

namespace Api.Repositories;

/// <summary>
/// Defines data access operations for user preferences.
/// </summary>
public interface IUserPreferenceRepository
{
    /// <summary>
    /// Retrieves a user's preferences by their user ID.
    /// </summary>
    Task<UserPreference?> GetByUserIdAsync(int userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new user preference record.
    /// </summary>
    Task<UserPreference> CreateAsync(UserPreference entity, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing user preference record.
    /// </summary>
    Task<UserPreference> UpdateAsync(UserPreference entity, CancellationToken cancellationToken);
}
