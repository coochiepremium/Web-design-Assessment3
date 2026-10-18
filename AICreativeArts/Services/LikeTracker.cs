using System.Security.Claims;
using AICreativeArts.Authorization;
using AICreativeArts.Data;
using AICreativeArts.Models;
using Microsoft.EntityFrameworkCore;

namespace AICreativeArts.Services;

public enum LikeResult
{
    Liked,
    AlreadyLiked,
    NotFound,
    LoginExpired,
}

/// <summary>
/// The brief's like rule: a logged-in user may like each image once per login, and must
/// log out and in again to like it again. Likes are stored in the database against the
/// login id held in the sign-in cookie, so the rule survives page reloads, lost session
/// cookies and site restarts.
/// </summary>
public interface ILikeTracker
{
    /// <summary>The images the current login has already liked (empty when not signed in).</summary>
    Task<IReadOnlySet<int>> LikedImageIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>Records a like for the current login and increases the image's like count.</summary>
    Task<LikeResult> LikeAsync(int imageId, CancellationToken cancellationToken = default);
}

public class LoginLikeTracker : ILikeTracker
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<LoginLikeTracker> _logger;

    public LoginLikeTracker(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor, ILogger<LoginLikeTracker> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public async Task<IReadOnlySet<int>> LikedImageIdsAsync(CancellationToken cancellationToken = default)
    {
        var loginId = User?.GetLoginId();
        if (loginId is null)
        {
            return new HashSet<int>();
        }

        var ids = await _context.ImageLikes
            .Where(l => l.LoginId == loginId)
            .Select(l => l.AIImageId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }

    public async Task<LikeResult> LikeAsync(int imageId, CancellationToken cancellationToken = default)
    {
        var loginId = User?.GetLoginId();
        var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (loginId is null || userId is null)
        {
            // Signed in before login ids existed (or the cookie is incomplete): ask for a fresh login.
            return LikeResult.LoginExpired;
        }

        if (!await _context.AIImages.AnyAsync(i => i.Id == imageId, cancellationToken))
        {
            return LikeResult.NotFound;
        }

        if (await _context.ImageLikes.AnyAsync(l => l.LoginId == loginId && l.AIImageId == imageId, cancellationToken))
        {
            return LikeResult.AlreadyLiked;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _context.ImageLikes.Add(new ImageLike
            {
                AIImageId = imageId,
                LoginId = loginId,
                UserId = userId,
                LikedAt = DateTime.Now,
            });
            await _context.SaveChangesAsync(cancellationToken);

            // Increment in a single SQL statement so simultaneous likes from different users are never lost.
            await _context.AIImages
                .Where(i => i.Id == imageId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.Like, i => i.Like + 1), cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return LikeResult.Liked;
        }
        catch (DbUpdateException ex)
        {
            // The unique index caught a second like from this login arriving at the same moment.
            _logger.LogInformation(ex, "Duplicate like for image {ImageId} ignored.", imageId);
            await transaction.RollbackAsync(cancellationToken);
            return LikeResult.AlreadyLiked;
        }
    }
}
