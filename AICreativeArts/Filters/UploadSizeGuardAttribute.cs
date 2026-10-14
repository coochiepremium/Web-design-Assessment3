using AICreativeArts.Infrastructure;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AICreativeArts.Filters;

/// <summary>
/// Rejects oversized uploads before anything tries to bind the request body, so the
/// user is sent back to the form with a friendly message instead of an error page.
/// Runs ahead of the antiforgery check, which would otherwise read the body first.
/// The client-side check in site.js normally stops large files before they are sent;
/// this guard covers browsers with JavaScript turned off.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class UploadSizeGuardAttribute : Attribute, IAsyncAuthorizationFilter, IOrderedFilter
{
    /// <summary>Bodies up to this size are read and discarded so the browser receives the redirect.</summary>
    private const long DrainLimitBytes = 100L * 1024 * 1024;

    public int Order => -5000;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        var length = httpContext.Request.ContentLength;
        if (length is null || length <= UploadOptions.RequestLimitBytes)
        {
            return;
        }

        // A browser keeps sending the file until the server reads it; replying early makes it
        // show "connection reset". Draining the body lets it display the redirect instead.
        if (length <= DrainLimitBytes)
        {
            var sizeFeature = httpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (sizeFeature is { IsReadOnly: false })
            {
                sizeFeature.MaxRequestBodySize = length;
            }

            try
            {
                await httpContext.Request.Body.CopyToAsync(Stream.Null, httpContext.RequestAborted);
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException)
            {
                // The client gave up; nothing else to do.
            }
        }

        var tempData = httpContext.RequestServices
            .GetRequiredService<ITempDataDictionaryFactory>()
            .GetTempData(httpContext);
        tempData["SiteError"] = "That upload is far too large. Please choose an image smaller than 5 MB.";
        tempData.Save();

        var values = context.RouteData.Values;
        context.Result = new RedirectToActionResult(
            values["action"]?.ToString(),
            values["controller"]?.ToString(),
            values.TryGetValue("id", out var id) ? new { id } : null);
    }
}
