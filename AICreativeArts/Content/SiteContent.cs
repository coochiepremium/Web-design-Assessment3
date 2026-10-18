namespace AICreativeArts.Content;

/// <summary>
/// Researched copy for the home page (theme: the harmful side of AI in the creative arts).
/// Keeping it in one place keeps the Razor views free of long strings and lets the
/// Contact page list every source automatically.
/// </summary>
public static class SiteContent
{
    public const string HeroEyebrow = "Content warning: the hidden cost of generative AI";

    public const string HeroTitle = "The Dark Side of AI Art";

    public const string HeroText =
        "Generative AI can produce a painting, a song or a story in seconds, but that speed comes at a cost. " +
        "These systems are trained on millions of human-made works, often without consent, credit or payment, " +
        "and they now compete with the very artists whose work made them possible, while making convincing " +
        "fakes easier than ever.";

    public const string HeroSource = "Adapted from Wikipedia";

    public const string HeroSourceUrl = "https://en.wikipedia.org/wiki/AI_art";

    public static IReadOnlyList<FeatureSection> Sections { get; } = new List<FeatureSection>
    {
        new(
            Id: "art-without-consent",
            CaseLabel: "Case 01 · Consent & copyright",
            Title: "Art Without Consent",
            Description:
                "Many leading image generators, such as Stable Diffusion, were trained on billions of pictures " +
                "scraped from the internet, including copyrighted artwork, mostly without the artists' permission, " +
                "credit or payment.",
            KeyFact: "5.85 billion image–text pairs were collected for the LAION-5B training dataset.",
            ReadMoreUrl: "https://en.wikipedia.org/wiki/Artificial_intelligence_and_copyright",
            TextFirst: true,
            Examples: new List<CaseExample>
            {
                new("LAION-5B", "tile-laion",
                    "LAION-5B is an open dataset of about 5.85 billion image–text pairs scraped from the web, " +
                    "released in 2022 and used to train image generators such as Stable Diffusion.",
                    "LAION", "https://laion.ai/blog/laion-5b/"),
                new("Getty Images v Stability AI", "tile-getty",
                    "In 2023 Getty Images sued Stability AI, claiming it copied over 12 million Getty photos without " +
                    "permission to train Stable Diffusion. A UK court largely sided with Stability in 2025; a US case continues.",
                    "Wikipedia", "https://en.wikipedia.org/wiki/Stability_AI"),
            }),
        new(
            Id: "livelihoods-at-risk",
            CaseLabel: "Case 02 · Jobs & income",
            Title: "Livelihoods at Risk",
            Description:
                "Some studios, publishers and game companies have turned to AI to cut costs, reducing work for " +
                "illustrators, voice actors and writers, or paying them less to fix machine output.",
            KeyFact: "118 days: the 2023 SAG-AFTRA actors' strike, fought partly over AI digital replicas.",
            ReadMoreUrl: "https://en.wikipedia.org/wiki/Generative_AI",
            TextFirst: false,
            Examples: new List<CaseExample>
            {
                new("2023 Writers Guild of America strike", "tile-wga",
                    "The 2023 Writers Guild of America strike lasted 148 days and won rules that AI cannot write " +
                    "or rewrite literary material, and that AI-generated text cannot count as source material.",
                    "Wikipedia", "https://en.wikipedia.org/wiki/2023_Writers_Guild_of_America_strike"),
                new("2023 SAG-AFTRA strike", "tile-sag",
                    "The 2023 SAG-AFTRA strike lasted 118 days; actors won rules requiring their consent and " +
                    "payment before studios create or use digital replicas of their faces and voices.",
                    "Wikipedia", "https://en.wikipedia.org/wiki/2023_SAG-AFTRA_strike"),
            }),
        new(
            Id: "fakes-and-deception",
            CaseLabel: "Case 03 · Authenticity",
            Title: "Fakes and Deception",
            Description:
                "Voice cloning and image generators make it easy to imitate a singer, actor or painter, " +
                "flooding platforms with convincing fakes that mislead audiences and damage reputations.",
            KeyFact: "'Heart on My Sleeve' gained millions of plays in under two weeks before it was taken down.",
            ReadMoreUrl: "https://en.wikipedia.org/wiki/Deepfake",
            TextFirst: true,
            Examples: new List<CaseExample>
            {
                new("Heart on My Sleeve", "tile-heart",
                    "In April 2023 'Heart on My Sleeve', a song using AI-cloned voices of Drake and The Weeknd, " +
                    "went viral before Universal Music Group had it removed from streaming services.",
                    "Wikipedia", "https://en.wikipedia.org/wiki/Heart_on_My_Sleeve_(Ghostwriter977_song)"),
                new("Théâtre D'opéra Spatial", "tile-theatre",
                    "In 2022 a Midjourney image won first place in a digital arts category at the Colorado State " +
                    "Fair, angering artists; in 2023 the US Copyright Office refused to register it.",
                    "Wikipedia", "https://en.wikipedia.org/wiki/Th%C3%A9%C3%A2tre_D%27op%C3%A9ra_Spatial"),
            }),
    };

    /// <summary>Every external text source used on the site, credited on the Contact page.</summary>
    public static IReadOnlyList<(string Title, string Url)> TextSources { get; } = new List<(string, string)>
    {
        ("Wikipedia – AI art", HeroSourceUrl),
        ("Wikipedia – Artificial intelligence and copyright", "https://en.wikipedia.org/wiki/Artificial_intelligence_and_copyright"),
        ("LAION – LAION-5B announcement", "https://laion.ai/blog/laion-5b/"),
        ("Wikipedia – Stable Diffusion", "https://en.wikipedia.org/wiki/Stable_Diffusion"),
        ("Wikipedia – Stability AI (Getty Images lawsuit)", "https://en.wikipedia.org/wiki/Stability_AI"),
        ("Wikipedia – Generative AI (impact on creative jobs)", "https://en.wikipedia.org/wiki/Generative_AI"),
        ("Wikipedia – 2023 Writers Guild of America strike", "https://en.wikipedia.org/wiki/2023_Writers_Guild_of_America_strike"),
        ("Wikipedia – 2023 SAG-AFTRA strike", "https://en.wikipedia.org/wiki/2023_SAG-AFTRA_strike"),
        ("Wikipedia – Deepfake", "https://en.wikipedia.org/wiki/Deepfake"),
        ("Wikipedia – Heart on My Sleeve (Ghostwriter977 song)", "https://en.wikipedia.org/wiki/Heart_on_My_Sleeve_(Ghostwriter977_song)"),
        ("Wikipedia – Théâtre D'opéra Spatial", "https://en.wikipedia.org/wiki/Th%C3%A9%C3%A2tre_D%27op%C3%A9ra_Spatial"),
        ("Glaze – University of Chicago", "https://glaze.cs.uchicago.edu/"),
        ("Nightshade – University of Chicago", "https://nightshade.cs.uchicago.edu/"),
        ("OpenAI – GPTBot documentation", "https://platform.openai.com/docs/gptbot"),
        ("Wikipedia – Artificial Intelligence Act (EU)", "https://en.wikipedia.org/wiki/Artificial_Intelligence_Act"),
        ("US Copyright Office – Copyright and Artificial Intelligence", "https://www.copyright.gov/ai/"),
    };
}
