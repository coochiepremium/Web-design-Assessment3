using System.ComponentModel.DataAnnotations;
using AICreativeArts.Models;

namespace AICreativeArts.ViewModels;

/// <summary>
/// Form model shared by the Create and Edit pages. Only Prompt, ImageGenerator and
/// ImageFile are accepted from the browser; the upload date, like count and stored
/// filename are always set on the server so they cannot be tampered with.
/// </summary>
public class AIImageFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the prompt used to generate the image.")]
    [StringLength(AIImageRules.PromptMaxLength, MinimumLength = 5,
        ErrorMessage = "The prompt must be between {2} and {1} characters.")]
    public string Prompt { get; set; } = "";

    [Required(ErrorMessage = "Please enter the image generator (for example Midjourney).")]
    [StringLength(AIImageRules.GeneratorMaxLength, MinimumLength = 2,
        ErrorMessage = "The image generator must be between {2} and {1} characters.")]
    [Display(Name = "Image Generator")]
    public string ImageGenerator { get; set; } = "";

    [Display(Name = "Upload Date")]
    public DateTime UploadDate { get; set; }

    public int Like { get; set; }

    [Display(Name = "Upload image file")]
    public IFormFile? ImageFile { get; set; }

    /// <summary>The image currently stored for this record (Edit only, display purposes).</summary>
    public string? CurrentFilename { get; set; }

    public static AIImageFormViewModel FromEntity(AIImage image) => new()
    {
        Id = image.Id,
        Prompt = image.Prompt,
        ImageGenerator = image.ImageGenerator,
        UploadDate = DateTime.Now,
        Like = image.Like,
        CurrentFilename = image.Filename,
    };
}
