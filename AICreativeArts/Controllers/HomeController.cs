using System.Diagnostics;
using AICreativeArts.Infrastructure;
using AICreativeArts.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AICreativeArts.Controllers;

/// <summary>Public pages: Home, Contact, Artist Shield and the friendly error pages.</summary>
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly SiteOptions _site;

    public HomeController(ILogger<HomeController> logger, IOptions<SiteOptions> site)
    {
        _logger = logger;
        _site = site.Value;
    }

    public IActionResult Index() => View();

    public IActionResult Contact() => View(_site);

    /// <summary>The additional creative page: a quiz, a protection checklist and a timeline for artists.</summary>
    public IActionResult ArtistShield() => View();

    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (feature is not null)
        {
            _logger.LogError(feature.Error, "Unhandled exception on {Path}", feature.Path);
        }

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Status(int code)
    {
        var feature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        if (feature is null)
        {
            // Visited directly rather than re-executed after an error.
            return RedirectToAction(nameof(Index));
        }

        Response.StatusCode = code is >= 400 and < 600 ? code : 500;

        // Missing files (an image slot not filled yet, a deleted upload) get a plain status
        // code: the browser asked for an image or script, not a page.
        if (Path.HasExtension(feature.OriginalPath))
        {
            return new StatusCodeResult(Response.StatusCode);
        }

        return View(StatusCodeViewModel.For(code, feature.OriginalPath));
    }
}
