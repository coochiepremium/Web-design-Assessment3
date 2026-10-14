using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AICreativeArts.Areas.Identity.Pages.Account;

/// <summary>Shown when a signed-in member tries to open an admin-only page.</summary>
public class AccessDeniedModel : PageModel
{
    public string? AttemptedUrl { get; private set; }

    public void OnGet(string? returnUrl = null)
    {
        AttemptedUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : null;
    }
}
