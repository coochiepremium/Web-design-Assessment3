namespace AICreativeArts.Infrastructure;

/// <summary>Site owner details shown on the Contact page (configured in appsettings.json).</summary>
public class SiteOptions
{
    public const string SectionName = "Site";

    public string Name { get; set; } = "MuseAI";

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
