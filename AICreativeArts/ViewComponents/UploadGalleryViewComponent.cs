using AICreativeArts.Data;
using AICreativeArts.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AICreativeArts.ViewComponents;

public record UploadGalleryViewModel(IReadOnlyList<AIImage> Images, int TotalCount, bool Unavailable = false);

/// <summary>
/// The "Upload AI images" tiles on the home page, most-liked first. If the database
/// cannot be reached the rest of the home page still renders with a short notice.
/// </summary>
public class UploadGalleryViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UploadGalleryViewComponent> _logger;

    public UploadGalleryViewComponent(ApplicationDbContext context, ILogger<UploadGalleryViewComponent> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync(int maxItems = 24)
    {
        try
        {
            var images = await _context.AIImages
                .AsNoTracking()
                .OrderByDescending(i => i.Like)
                .ThenByDescending(i => i.UploadDate)
                .Take(maxItems)
                .ToListAsync(HttpContext.RequestAborted);
            var total = await _context.AIImages.CountAsync(HttpContext.RequestAborted);
            return View(new UploadGalleryViewModel(images, total));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Could not load the upload gallery.");
            return View(new UploadGalleryViewModel(Array.Empty<AIImage>(), 0, Unavailable: true));
        }
    }
}
