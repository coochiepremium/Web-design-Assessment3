namespace AICreativeArts.Content;

public enum ImageSlotKind
{
    /// <summary>Shown with an &lt;img&gt; tag (via the &lt;image-slot&gt; tag helper).</summary>
    Image,

    /// <summary>Used as a CSS background-image in site.css.</summary>
    Background,

    /// <summary>Browser tab icon.</summary>
    Icon,
}

/// <summary>
/// One place on the site where an image belongs. Put a file at <see cref="File"/>
/// (relative to wwwroot) and it appears automatically; until then a placeholder shows
/// <see cref="Description"/> so you know exactly what to add. Credit the image in the
/// "ImageCredits" section of appsettings.json, using the same <see cref="Key"/>.
/// </summary>
public record ImageSlot(
    string Key,
    string File,
    ImageSlotKind Kind,
    string UsedOn,
    string Description,
    string Recommended,
    string Alt);

/// <summary>Every image slot on the website. The Contact page lists them all with their credits.</summary>
public static class ImageLibrary
{
    public const string HeroBackground = "hero-background";
    public const string SectionBackground = "section-background";
    public const string UploadPreview = "upload-preview";
    public const string Favicon = "favicon";

    public static IReadOnlyList<ImageSlot> Slots { get; } = new List<ImageSlot>
    {
        new(HeroBackground, "img/backgrounds/hero.jpg", ImageSlotKind.Background,
            "Top banner of Home, AI Images, Artist Shield and the whole Contact page (fixed parallax background)",
            "A dark, moody wide image about the harm AI can do to art, e.g. a cracked or glitched painting, " +
            "an empty artist's studio, or a canvas fading into pixels. It must be dark enough for white text.",
            "Landscape JPG, at least 1920 × 1080 px, under 600 KB",
            "Dark background image"),

        new(SectionBackground, "img/backgrounds/section.jpg", ImageSlotKind.Background,
            "Behind the three case blocks and upload gallery on Home, plus the Create, Edit, Delete, Details and error pages",
            "A subtle, dark texture that will not compete with text, e.g. a close-up of canvas or paper, " +
            "a paint-stained desk in shadow, or blurred lights of a server room.",
            "Landscape JPG, at least 1920 × 1280 px, under 600 KB",
            "Dark textured background"),

        new("tile-laion", "img/tiles/laion-5b.jpg", ImageSlotKind.Image,
            "Home – case 01 'Art Without Consent', first square",
            "Represents the LAION-5B training dataset: a mosaic of many tiny artworks, or rows of data on a screen.",
            "Square JPG, at least 600 × 600 px",
            "Mosaic representing billions of scraped images"),

        new("tile-getty", "img/tiles/getty-lawsuit.jpg", ImageSlotKind.Image,
            "Home – case 01 'Art Without Consent', second square",
            "Represents the Getty Images v Stability AI lawsuit: a gavel, a courtroom, or a photo with a watermark. " +
            "Do not use the Getty Images logo unless you have permission.",
            "Square JPG, at least 600 × 600 px",
            "Courtroom gavel representing the Getty Images lawsuit"),

        new("tile-wga", "img/tiles/wga-strike.jpg", ImageSlotKind.Image,
            "Home – case 02 'Livelihoods at Risk', first square",
            "Represents the 2023 Hollywood writers' strike: picket signs, a typewriter, or a crowd of protesters.",
            "Square JPG, at least 600 × 600 px",
            "Writers on a picket line"),

        new("tile-sag", "img/tiles/sag-aftra-strike.jpg", ImageSlotKind.Image,
            "Home – case 02 'Livelihoods at Risk', second square",
            "Represents the 2023 actors' strike and digital replicas: a picket line, a theatre mask, or a face being scanned.",
            "Square JPG, at least 600 × 600 px",
            "Actors' strike and digital face scanning"),

        new("tile-heart", "img/tiles/heart-on-my-sleeve.jpg", ImageSlotKind.Image,
            "Home – case 03 'Fakes and Deception', first square",
            "Represents AI voice cloning: a studio microphone, a sound wave, or headphones. " +
            "Avoid photos of Drake or The Weeknd unless they are licensed for reuse.",
            "Square JPG, at least 600 × 600 px",
            "Studio microphone representing a cloned voice"),

        new("tile-theatre", "img/tiles/theatre-dopera-spatial.jpg", ImageSlotKind.Image,
            "Home – case 03 'Fakes and Deception', second square",
            "Represents the AI image that won an art prize: an award ribbon, a gallery wall, or judges at a contest. " +
            "Do not use the original artwork unless you have permission.",
            "Square JPG, at least 600 × 600 px",
            "Award ribbon at an art competition"),

        new(UploadPreview, "img/ui/upload-preview.jpg", ImageSlotKind.Image,
            "Create page – default image shown before a file is chosen",
            "A neutral 'your image here' picture, e.g. an empty frame on a wall or a blank canvas on an easel.",
            "Square JPG, at least 600 × 600 px",
            "Empty frame waiting for an uploaded image"),

        new(Favicon, "favicon.ico", ImageSlotKind.Icon,
            "Browser tab icon on every page",
            "A small square icon for the site, e.g. a shield or a warning sign. Convert a PNG to .ico with any favicon generator.",
            "ICO file containing 16 × 16, 32 × 32 and 48 × 48 px",
            "Site icon"),
    };

    public static ImageSlot? Find(string key) =>
        Slots.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));
}
