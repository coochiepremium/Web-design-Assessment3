using AICreativeArts.Authorization;
using AICreativeArts.Data;
using AICreativeArts.Filters;
using AICreativeArts.Infrastructure;
using AICreativeArts.Models;
using AICreativeArts.Services;
using AICreativeArts.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AICreativeArts.Controllers;

/// <summary>
/// AI image gallery. Access rules:
///   Visitor - can browse the list (no buttons).
///   Member  - can browse, like and upload (Create).
///   Admin   - everything, including Details, Edit and Delete.
/// </summary>
public class AIImagesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IImageStorage _storage;
    private readonly ILikeTracker _likes;
    private readonly ILogger<AIImagesController> _logger;

    public AIImagesController(ApplicationDbContext context, IImageStorage storage, ILikeTracker likes,
        ILogger<AIImagesController> logger)
    {
        _context = context;
        _storage = storage;
        _likes = likes;
        _logger = logger;
    }

    // GET: AIImages
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var images = await _context.AIImages
            .AsNoTracking()
            .OrderByDescending(i => i.Like)
            .ThenByDescending(i => i.UploadDate)
            .ToListAsync();

        var signedIn = User.Identity?.IsAuthenticated == true;
        foreach (var image in images)
        {
            image.canIncreaseLike = signedIn && !_likes.HasLiked(image.Id);
        }

        return View(images);
    }

    // GET: AIImages/Details/5
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Details(int? id)
    {
        var image = id is null ? null : await _context.AIImages.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return image is null ? NotFound() : View(image);
    }

    // GET: AIImages/Create
    [Authorize]
    public IActionResult Create(string? prompt)
    {
        var model = new AIImageFormViewModel
        {
            UploadDate = DateTime.Now,
            Like = 0,
            Prompt = Truncate(prompt?.Trim(), AIImageRules.PromptMaxLength) ?? "",
        };
        return View(model);
    }

    // POST: AIImages/Create
    [HttpPost]
    [Authorize]
    [UploadSizeGuard]
    [RequestSizeLimit(UploadOptions.RequestLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadOptions.RequestLimitBytes)]
    public async Task<IActionResult> Create(AIImageFormViewModel model)
    {
        // Values the user cannot change are always set on the server.
        model.UploadDate = DateTime.Now;
        model.Like = 0;

        if (model.ImageFile is null)
        {
            ModelState.AddModelError(nameof(model.ImageFile), "Please choose an image file to upload.");
        }
        else if (await _storage.ValidateAsync(model.ImageFile) is { } fileError)
        {
            ModelState.AddModelError(nameof(model.ImageFile), fileError);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string filename;
        try
        {
            filename = await _storage.SaveAsync(model.ImageFile!);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Saving an uploaded image failed.");
            ModelState.AddModelError(string.Empty, "Your image could not be saved. Please try again.");
            return View(model);
        }

        var image = new AIImage
        {
            Prompt = model.Prompt.Trim(),
            ImageGenerator = model.ImageGenerator.Trim(),
            UploadDate = model.UploadDate,
            Filename = filename,
            Like = 0,
        };

        try
        {
            _context.AIImages.Add(image);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Saving a new AI image record failed.");
            _storage.Delete(filename);
            ModelState.AddModelError(string.Empty, "Your image could not be saved to the gallery. Please try again.");
            return View(model);
        }

        TempData["SiteMessage"] = $"Thanks! Your {image.ImageGenerator} image has been uploaded.";
        return Redirect(Url.Action(nameof(HomeController.Index), "Home", values: null, protocol: null, host: null, fragment: "upload-ai-images")!);
    }

    // GET: AIImages/Edit/5
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int? id)
    {
        var image = id is null ? null : await _context.AIImages.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return image is null ? NotFound() : View(AIImageFormViewModel.FromEntity(image));
    }

    // POST: AIImages/Edit/5
    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [UploadSizeGuard]
    [RequestSizeLimit(UploadOptions.RequestLimitBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = UploadOptions.RequestLimitBytes)]
    public async Task<IActionResult> Edit(int id, AIImageFormViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        var image = await _context.AIImages.FirstOrDefaultAsync(i => i.Id == id);
        if (image is null)
        {
            return NotFound();
        }

        // Display-only values come from the database, never from the form.
        model.UploadDate = DateTime.Now;
        model.Like = image.Like;
        model.CurrentFilename = image.Filename;

        if (model.ImageFile is not null && await _storage.ValidateAsync(model.ImageFile) is { } fileError)
        {
            ModelState.AddModelError(nameof(model.ImageFile), fileError);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string? newFilename = null;
        if (model.ImageFile is not null)
        {
            try
            {
                newFilename = await _storage.SaveAsync(model.ImageFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogError(ex, "Saving a replacement image for {Id} failed.", id);
                ModelState.AddModelError(string.Empty, "The new image could not be saved. Please try again.");
                return View(model);
            }
        }

        var oldFilename = image.Filename;
        image.Prompt = model.Prompt.Trim();
        image.ImageGenerator = model.ImageGenerator.Trim();
        image.UploadDate = model.UploadDate;
        if (newFilename is not null)
        {
            image.Filename = newFilename;
        }

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _storage.Delete(newFilename);
            if (!await _context.AIImages.AnyAsync(i => i.Id == id))
            {
                return NotFound();
            }

            ModelState.AddModelError(string.Empty, "This image was changed by someone else. Please reload and try again.");
            return View(model);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Updating AI image {Id} failed.", id);
            _storage.Delete(newFilename);
            ModelState.AddModelError(string.Empty, "Your changes could not be saved. Please try again.");
            return View(model);
        }

        if (newFilename is not null)
        {
            _storage.Delete(oldFilename);
        }

        TempData["SiteMessage"] = "The image has been updated.";
        return Redirect(Url.ImageAnchor(image.Id));
    }

    // GET: AIImages/Delete/5
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int? id)
    {
        var image = id is null ? null : await _context.AIImages.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id);
        return image is null ? NotFound() : View(image);
    }

    // POST: AIImages/Delete/5
    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var image = await _context.AIImages.FindAsync(id);
        if (image is null)
        {
            TempData["SiteMessage"] = "That image had already been deleted.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            _context.AIImages.Remove(image);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Deleting AI image {Id} failed.", id);
            TempData["SiteError"] = "The image could not be deleted. Please try again.";
            return RedirectToAction(nameof(Delete), new { id });
        }

        _storage.Delete(image.Filename);
        TempData["SiteMessage"] = $"The {image.ImageGenerator} image has been deleted.";
        return RedirectToAction(nameof(Index));
    }

    // POST: AIImages/Like/5
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Like(int id)
    {
        if (_likes.HasLiked(id))
        {
            TempData["SiteMessage"] = "You have already liked this image. Log out and back in to like it again.";
            return Redirect(Url.ImageAnchor(id));
        }

        // Increment in a single SQL statement so simultaneous likes are never lost.
        var updated = await _context.AIImages
            .Where(i => i.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.Like, i => i.Like + 1));
        if (updated == 0)
        {
            return NotFound();
        }

        _likes.RecordLike(id);
        return Redirect(Url.ImageAnchor(id));
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength] : value;
}
