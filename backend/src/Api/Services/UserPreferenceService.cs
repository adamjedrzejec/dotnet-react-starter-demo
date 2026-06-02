using Api.DTOs.Common;
using Api.DTOs.UserPreferences;
using Api.Models;
using Api.Repositories;

namespace Api.Services;

/// <summary>
/// Business logic for managing user preferences with envelope responses.
/// </summary>
public class UserPreferenceService : IUserPreferenceService
{
    private readonly IUserPreferenceRepository _repository;

    public UserPreferenceService(IUserPreferenceRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<ItemResponseDto<UserPreferenceDto>?> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByUserIdAsync(userId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        return WrapInEnvelope(entity);
    }

    /// <inheritdoc/>
    public async Task<ItemResponseDto<UserPreferenceDto>> UpdateAsync(
        int userId,
        UpdateUserPreferenceRequestDto request,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByUserIdAsync(userId, cancellationToken);

        UserPreference entity;
        if (existing is null)
        {
            entity = new UserPreference
            {
                UserId = userId,
                ThemePreference = request.ThemePreference,
                EmailNotificationIndicator = request.EmailNotificationIndicator,
                PushNotificationIndicator = request.PushNotificationIndicator,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            await _repository.CreateAsync(entity, cancellationToken);
        }
        else
        {
            existing.ThemePreference = request.ThemePreference;
            existing.EmailNotificationIndicator = request.EmailNotificationIndicator;
            existing.PushNotificationIndicator = request.PushNotificationIndicator;
            existing.UpdatedDate = DateTime.UtcNow;
            entity = await _repository.UpdateAsync(existing, cancellationToken);
        }

        return WrapInEnvelope(entity);
    }

    private static ItemResponseDto<UserPreferenceDto> WrapInEnvelope(UserPreference entity)
    {
        return new ItemResponseDto<UserPreferenceDto>
        {
            Item = new UserPreferenceDto
            {
                UserPreferenceId = entity.UserPreferenceId,
                UserId = entity.UserId,
                ThemePreference = entity.ThemePreference,
                EmailNotificationIndicator = entity.EmailNotificationIndicator,
                PushNotificationIndicator = entity.PushNotificationIndicator,
                CreatedDate = entity.CreatedDate,
                UpdatedDate = entity.UpdatedDate
            },
            Metadata = new MetadataDto
            {
                Timestamp = DateTime.UtcNow,
                TransactionId = Guid.NewGuid().ToString(),
                TotalCount = null
            },
            Links = new LinksDto
            {
                Self = $"/v1/userpreferences/{entity.UserId}",
                Next = null,
                Prev = null
            }
        };
    }
}
