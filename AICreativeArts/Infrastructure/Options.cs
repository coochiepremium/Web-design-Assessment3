namespace AICreativeArts.Infrastructure;

/// <summary>Site owner details shown on the Contact page (configured in appsettings.json).</summary>
public class SiteOptions
{
    public const string SectionName = "Site";

    public string Name { get; set; } = "AI Unmasked";

    public string DeveloperName { get; set; } = "";

    public string ContactEmail { get; set; } = "";
}

/// <summary>Rules for uploaded AI images.</summary>
public class UploadOptions
{
    public const string SectionName = "Uploads";

    /// <summary>Folder under wwwroot where uploaded images are stored.</summary>
    public string FolderName { get; set; } = "uploads";

    /// <summary>Largest accepted image, in bytes (default 5 MB).</summary>
    public long MaxFileBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>
    /// Hard limit for the whole request. Kept above <see cref="MaxFileBytes"/> so slightly
    /// larger files still reach the action and get a friendly validation message.
    /// </summary>
    public const long RequestLimitBytes = 10 * 1024 * 1024;
}

/// <summary>Accounts created at start-up so the marker can try every role straight away.</summary>
public class SeedAccountOptions
{
    public const string SectionName = "SeedAccounts";

    public List<SeedAccount> Accounts { get; set; } = new();
}

public class SeedAccount
{
    public string Email { get; set; } = "";

    public string Password { get; set; } = "";

    /// <summary>Role to assign, or empty for a member (no role).</summary>
    public string? Role { get; set; }
}

/// <summary>
/// Credit for one image slot, configured in the "ImageCredits" section of appsettings.json
/// using the slot's key (see Content/ImageLibrary.cs). Shown on the Contact page.
/// </summary>
public class ImageCredit
{
    /// <summary>Who made the image, e.g. "Jane Doe" (or "Your name" for your own photo).</summary>
    public string Creator { get; set; } = "";

    /// <summary>Where it came from, e.g. "Unsplash", "Pexels", "Wikimedia Commons" or "Own work".</summary>
    public string Source { get; set; } = "";

    /// <summary>Link to the original page of the image.</summary>
    public string Url { get; set; } = "";

    /// <summary>The licence, e.g. "Unsplash Licence", "CC BY 4.0" or "Public domain".</summary>
    public string Licence { get; set; } = "";

    public bool IsComplete => !string.IsNullOrWhiteSpace(Creator) && !string.IsNullOrWhiteSpace(Source);

    public bool HasLink => Uri.TryCreate(Url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}

/// <summary>All image credits, keyed by image slot key (case-insensitive).</summary>
public class ImageCreditOptions : Dictionary<string, ImageCredit>
{
    public const string SectionName = "ImageCredits";

    public ImageCreditOptions()
        : base(StringComparer.OrdinalIgnoreCase)
    {
    }
}
