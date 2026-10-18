using System.Security.Claims;

namespace AICreativeArts.Authorization;

/// <summary>
/// Every successful sign-in is given a new random "login id" claim inside the sign-in
/// cookie. Likes are recorded against it, which gives the brief's rule "one like per
/// image per login: log out and in again to like again".
/// </summary>
public static class LoginSession
{
    public const string ClaimType = "aiunmasked:login-id";

    /// <summary>Adds a new login id to a principal that is signing in (if it has none yet).</summary>
    public static void AddLoginId(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is ClaimsIdentity identity && !identity.HasClaim(c => c.Type == ClaimType))
        {
            identity.AddClaim(new Claim(ClaimType, Guid.NewGuid().ToString("N")));
        }
    }

    /// <summary>Keeps the same login id when Identity periodically rebuilds the sign-in cookie.</summary>
    public static void CopyLoginId(ClaimsPrincipal? from, ClaimsPrincipal? to)
    {
        var loginId = from?.FindFirst(ClaimType);
        if (loginId is not null && to?.Identity is ClaimsIdentity identity && !identity.HasClaim(c => c.Type == ClaimType))
        {
            identity.AddClaim(new Claim(ClaimType, loginId.Value));
        }
    }

    public static string? GetLoginId(this ClaimsPrincipal user) => user.FindFirstValue(ClaimType);
}
