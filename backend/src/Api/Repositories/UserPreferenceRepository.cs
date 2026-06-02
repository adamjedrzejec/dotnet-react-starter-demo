using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

/// <summary>
/// EF Core implementation of user preference data access.
/// </summary>
public class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly AppDbContext _context;

    public UserPreferenceRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<UserPreference?> GetByUserIdAsync(int userId, CancellationToken cancellationToken)
    {
        return await _context.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserPreference> CreateAsync(UserPreference entity, CancellationToken cancellationToken)
    {
        _context.UserPreferences.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc/>
    public async Task<UserPreference> UpdateAsync(UserPreference entity, CancellationToken cancellationToken)
    {
        _context.UserPreferences.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
