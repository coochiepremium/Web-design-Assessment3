namespace AICreativeArts.Content;

/// <summary>A multiple-choice question; <see cref="CorrectIndex"/> points into <see cref="Options"/>.</summary>
public record QuizQuestion(
    string Id,
    string Question,
    IReadOnlyList<string> Options,
    int CorrectIndex,
    string Explanation,
    string SourceUrl);

/// <summary>A practical step artists can take; ticked steps are remembered in the browser.</summary>
public record ShieldStep(string Id, string Title, string Detail, string? LinkText, string? LinkUrl);

public record TimelineEntry(string Date, string Title, string Text, string SourceUrl);

/// <summary>Data for the Artist Shield page (the additional creative page).</summary>
public static class ArtistShieldContent
{
    public static IReadOnlyList<QuizQuestion> Quiz { get; } = new List<QuizQuestion>
    {
        new("q-laion", "About how many image–text pairs were collected for the LAION-5B dataset?",
            new[] { "5 million", "585 million", "5.85 billion" }, 2,
            "LAION-5B holds about 5.85 billion image–text pairs gathered from the public web, including artwork shared online.",
            "https://laion.ai/blog/laion-5b/"),
        new("q-sag", "How long did the 2023 SAG-AFTRA actors' strike last?",
            new[] { "30 days", "118 days", "One year" }, 1,
            "The strike lasted 118 days, and AI digital replicas were one of the last issues to be settled.",
            "https://en.wikipedia.org/wiki/2023_SAG-AFTRA_strike"),
        new("q-glaze", "What does the Glaze tool do for artists?",
            new[] { "Deletes their art from the internet", "Adds subtle changes that make their style harder for AI to copy", "Sues AI companies automatically" }, 1,
            "Glaze makes small changes to an image that people barely notice, but that make it harder for AI models to mimic the artist's style. It is not a permanent fix, so artists combine it with other protections.",
            "https://glaze.cs.uchicago.edu/"),
        new("q-heart", "Why was 'Heart on My Sleeve' pulled from streaming services in 2023?",
            new[] { "It used AI-cloned voices of Drake and The Weeknd", "It was too long", "Its lyrics were offensive" }, 0,
            "The song imitated Drake and The Weeknd with AI voice clones, and Universal Music Group had it taken down.",
            "https://en.wikipedia.org/wiki/Heart_on_My_Sleeve_(Ghostwriter977_song)"),
        new("q-copyright", "What did the US Copyright Office decide about the AI image Théâtre D'opéra Spatial?",
            new[] { "It gave the image full copyright", "It refused to register it", "It banned Midjourney" }, 1,
            "It refused registration because the image contained more than a minimal amount of AI-generated material that the applicant would not exclude.",
            "https://en.wikipedia.org/wiki/Th%C3%A9%C3%A2tre_D%27op%C3%A9ra_Spatial"),
        new("q-wga", "Which group won rules in 2023 that AI cannot write or rewrite their scripts?",
            new[] { "The Writers Guild of America", "The Recording Academy", "The European Parliament" }, 0,
            "The 148-day WGA strike ended with rules that AI cannot write or rewrite literary material.",
            "https://en.wikipedia.org/wiki/2023_Writers_Guild_of_America_strike"),
    };

    public static IReadOnlyList<ShieldStep> Steps { get; } = new List<ShieldStep>
    {
        new("step-optout", "Use opt-out settings wherever they exist",
            "Some platforms and AI companies offer 'do not train' settings or opt-out forms. Many are voluntary, so treat them as one layer of protection, not the only one.",
            null, null),
        new("step-glaze", "Glaze images before you post them",
            "Glaze adds changes you can barely see, which make it harder for AI models to copy your style.",
            "Glaze", "https://glaze.cs.uchicago.edu/"),
        new("step-nightshade", "Consider Nightshade for extra protection",
            "Nightshade makes images that are scraped without permission teach AI models the wrong things.",
            "Nightshade", "https://nightshade.cs.uchicago.edu/"),
        new("step-robots", "Block AI crawlers from your portfolio website",
            "Add rules to your site's robots.txt file so crawlers such as OpenAI's GPTBot skip your pages.",
            "GPTBot documentation", "https://platform.openai.com/docs/gptbot"),
        new("step-lowres", "Share smaller, watermarked versions online",
            "Post lower-resolution copies with a visible signature and keep your full-size files offline.",
            null, null),
        new("step-terms", "Read a platform's terms before uploading",
            "Some sites allow your uploads to be used for AI training. Check the settings and opt out where you can.",
            null, null),
        new("step-records", "Keep a record of your creative process",
            "Save dated sketches, layered files and drafts. They help prove you are the human author of your work.",
            "Copyright and AI (US Copyright Office)", "https://www.copyright.gov/ai/"),
        new("step-collective", "Join other artists",
            "Unions, guilds and artist groups have won real protections, such as the 2023 Hollywood strike agreements.",
            null, null),
    };

    public static IReadOnlyList<TimelineEntry> Timeline { get; } = new List<TimelineEntry>
    {
        new("March 2022", "LAION-5B is released",
            "An open dataset of about 5.85 billion image–text pairs scraped from the web becomes the training ground for image generators.",
            "https://laion.ai/blog/laion-5b/"),
        new("August 2022", "An AI image wins an art prize",
            "Théâtre D'opéra Spatial, made with Midjourney, wins a digital arts category at the Colorado State Fair and sparks outrage among artists.",
            "https://en.wikipedia.org/wiki/Th%C3%A9%C3%A2tre_D%27op%C3%A9ra_Spatial"),
        new("January 2023", "Artists and Getty Images go to court",
            "Illustrators Sarah Andersen, Kelly McKernan and Karla Ortiz file a class action against Stability AI, Midjourney and DeviantArt, and Getty Images begins legal action against Stability AI.",
            "https://en.wikipedia.org/wiki/Artificial_intelligence_and_copyright"),
        new("March 2023", "Glaze is released",
            "University of Chicago researchers release Glaze, a free tool that makes artists' styles harder for AI to mimic.",
            "https://glaze.cs.uchicago.edu/"),
        new("April 2023", "A fake hit song goes viral",
            "'Heart on My Sleeve' imitates Drake and The Weeknd with AI voice clones before it is removed from streaming services.",
            "https://en.wikipedia.org/wiki/Heart_on_My_Sleeve_(Ghostwriter977_song)"),
        new("May – September 2023", "Hollywood writers strike",
            "The Writers Guild of America strikes for 148 days and wins limits on how studios can use AI.",
            "https://en.wikipedia.org/wiki/2023_Writers_Guild_of_America_strike"),
        new("July – November 2023", "Hollywood actors strike",
            "SAG-AFTRA strikes for 118 days and wins consent and pay rules for AI digital replicas.",
            "https://en.wikipedia.org/wiki/2023_SAG-AFTRA_strike"),
        new("January 2024", "Nightshade is released",
            "The Glaze team releases Nightshade, which 'poisons' images that are scraped without permission.",
            "https://nightshade.cs.uchicago.edu/"),
        new("August 2024", "The EU AI Act enters into force",
            "From August 2025, providers of general-purpose AI models must publish a summary of their training content and have a policy for complying with EU copyright law.",
            "https://en.wikipedia.org/wiki/Artificial_Intelligence_Act"),
    };
}
