using Api.DTOs.Common;
using Api.DTOs.UserPreferences;
using Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Manages user display and notification preferences.
/// </summary>
[Route("v1/[controller]")]
[ApiController]
[Produces("application/json")]
public class UserPreferencesController : ControllerBase
{
    private readonly IUserPreferenceService _service;

    /// <summary>
    /// Initializes a new instance of the user preferences controller.
    /// </summary>
    /// <param name="service">User preference service for business logic.</param>
    public UserPreferencesController(IUserPreferenceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Retrieves display and notification preferences for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>User preferences wrapped in the standard envelope.</returns>
    [HttpGet("{userId:int}")]
    [ProducesResponseType(typeof(ItemResponseDto<UserPreferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ItemResponseDto<UserPreferenceDto>>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByUserIdAsync(userId, cancellationToken);

        if (result is null)
        {
            return NotFound(new ErrorResponseDto
            {
                Code = "ORG-NTF-001",
                Message = $"User preferences not found for user {userId}.",
                Details = null
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Updates or creates display and notification preferences for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="request">The preference values to save.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <returns>Updated user preferences wrapped in the standard envelope.</returns>
    [HttpPut("{userId:int}")]
    [ProducesResponseType(typeof(ItemResponseDto<UserPreferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ItemResponseDto<UserPreferenceDto>>> UpdateAsync(
        int userId,
        [FromBody] UpdateUserPreferenceRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(userId, request, cancellationToken);
        return Ok(result);
    }
}
