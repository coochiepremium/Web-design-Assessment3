using Microsoft.AspNetCore.Http.Features;

namespace AICreativeArts.Services;

/// <summary>
/// Remembers which images the signed-in user has liked during the current login.
/// The brief allows one like per image per login: after logging out and back in the
/// user may like the image again, so the list lives in the session and is cleared
/// whenever the user signs in or out.
/// </summary>
public interface ILikeTracker
{
    bool HasLiked(int imageId);

    void RecordLike(int imageId);

    void Reset();
}

public class SessionLikeTracker : ILikeTracker
{
    private const string SessionKey = "AIUnmasked.LikedImageIds";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionLikeTracker(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool HasLiked(int imageId) => ReadIds().Contains(imageId);

    public void RecordLike(int imageId)
    {
        var session = GetSession();
        if (session is null)
        {
            return;
        }

        var ids = ReadIds();
        if (ids.Add(imageId))
        {
            session.SetString(SessionKey, string.Join(',', ids));
        }
    }

    public void Reset() => GetSession()?.Remove(SessionKey);

    private HashSet<int> ReadIds()
    {
        var value = GetSession()?.GetString(SessionKey);
        var ids = new HashSet<int>();
        if (string.IsNullOrEmpty(value))
        {
            return ids;
        }

        foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (int.TryParse(part, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    /// <summary>Returns the session, or null when session state is unavailable for this request.</summary>
    private ISession? GetSession()
    {
        var context = _httpContextAccessor.HttpContext;
        return context?.Features.Get<ISessionFeature>()?.Session;
    }
}
