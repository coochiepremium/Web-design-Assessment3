using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AICreativeArts.Models;

/// <summary>
/// An AI-generated image uploaded by a member. Shape follows the model given in the
/// assessment brief, with extra length limits so the database columns are bounded.
/// </summary>
public class AIImage
{
    public AIImage()
    {
        Prompt = "";
        ImageGenerator = "";
        Filename = "";
    }

    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(AIImageRules.PromptMaxLength)]
    public string Prompt { get; set; }

    [Required]
    [Display(Name = "Image Generator")]
    [StringLength(AIImageRules.GeneratorMaxLength)]
    public string ImageGenerator { get; set; }

    [Display(Name = "Upload Date")]
    public DateTime UploadDate { get; set; }

    [StringLength(AIImageRules.FilenameMaxLength)]
    public string Filename { get; set; }

    public int Like { get; set; }

    /// <summary>
    /// Whether the current viewer may still like this image. It depends on who is
    /// looking at the page, so it is calculated per request and never stored.
    /// </summary>
    [NotMapped]
    public bool canIncreaseLike { get; set; }
}

public static class AIImageRules
{
    public const int PromptMaxLength = 2000;
    public const int GeneratorMaxLength = 100;
    public const int FilenameMaxLength = 260;
}
