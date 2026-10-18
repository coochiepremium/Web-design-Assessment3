using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AICreativeArts.Filters;

/// <summary>
/// Makes the built-in ASP.NET Core Identity pages (the ones this project does not
/// override, e.g. two-factor sign-in and the Manage pages) fail politely:
/// <list type="bullet">
/// <item>Some of them throw <see cref="InvalidOperationException"/> when opened directly
/// (for example /Identity/Account/LoginWith2fa when no two-factor sign-in is in progress).
/// The user is redirected to a sensible page with a message instead of an error page.</item>
/// <item>Some return a plain-text 400/404 body. The body is dropped, so the site's
/// friendly status page is shown instead.</item>
/// </list>
/// </summary>
public sealed class IdentityPageGuardFilter : IAsyncPageFilter
{
    private readonly ITempDataDictionaryFactory _tempData;
    private readonly ILogger<IdentityPageGuardFilter> _logger;

    public IdentityPageGuardFilter(ITempDataDictionaryFactory tempData, ILogger<IdentityPageGuardFilter> logger)
    {
        _tempData = tempData;
        _logger = logger;
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var executed = await next();
        if (!IsIdentityPage(context.ActionDescriptor))
        {
            return;
        }

        if (executed.Exception is InvalidOperationException ex && !executed.ExceptionHandled)
        {
            var page = context.ActionDescriptor.ViewEnginePath;
            _logger.LogWarning(ex, "Identity page {Page} could not be shown; redirecting.", page);

            var tempData = _tempData.GetTempData(context.HttpContext);
            tempData["SiteError"] = "That account page isn't available right now. Please try again from here.";

            executed.ExceptionHandled = true;
            executed.Result = page.StartsWith("/Account/Manage", StringComparison.OrdinalIgnoreCase)
                ? new RedirectToPageResult("/Account/Manage/TwoFactorAuthentication", new { area = "Identity" })
                : new RedirectToPageResult("/Account/Login", new { area = "Identity" });
            return;
        }

        if (executed.Result is ObjectResult { StatusCode: >= 400 } error)
        {
            executed.Result = new StatusCodeResult(error.StatusCode!.Value);
        }
    }

    private static bool IsIdentityPage(CompiledPageActionDescriptor descriptor) =>
        string.Equals(descriptor.AreaName, "Identity", StringComparison.OrdinalIgnoreCase);
}
