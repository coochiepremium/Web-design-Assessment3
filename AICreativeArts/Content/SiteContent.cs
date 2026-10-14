namespace AICreativeArts.Content;

/// <summary>
/// Researched copy for the home page (theme: AI in Creative Arts). Keeping it in one
/// place keeps the Razor views free of long strings and lets the Contact page list
/// every source automatically.
/// </summary>
public static class SiteContent
{
    public const string HeroTitle = "AI in Creative Arts";

    public const string HeroText =
        "Creative artificial intelligence is the use of machine learning models to generate or transform " +
        "art, music and literature, often in response to text prompts. These models learn the patterns of " +
        "style, composition, melody and language in their training data and then produce new works that " +
        "artists can refine, remix and make their own.";

    public const string HeroSource = "Adapted from Wikipedia";

    public static IReadOnlyList<FeatureSection> Sections { get; } = new List<FeatureSection>
    {
        new(
            Id: "ai-art-generators",
            Title: "AI Art Generators",
            Description:
                "AI art generators are apps that turn a written description, called a prompt, into paintings, " +
                "illustrations or photorealistic images using text-to-image models, such as diffusion models, " +
                "trained on huge collections of captioned images.",
            ReadMoreUrl: "https://en.wikipedia.org/wiki/Text-to-image_model",
            TextFirst: true,
            Tools: new List<CreativeTool>
            {
                new("Midjourney", "~/img/tools/midjourney.jpg",
                    "Midjourney is a generative artificial intelligence program that creates images from natural " +
                    "language descriptions, called prompts, developed by the San Francisco-based lab Midjourney, Inc.",
                    "Wikipedia", "https://www.midjourney.com/"),
                new("Adobe Firefly", "~/img/tools/adobe-firefly.jpg",
                    "Adobe Firefly is a family of generative AI models from Adobe that creates and edits images " +
                    "from text prompts, and is built into Photoshop and Illustrator.",
                    "Wikipedia", "https://www.adobe.com/products/firefly.html"),
            }),
        new(
            Id: "ai-music-composition",
            Title: "AI Music Composition",
            Description:
                "AI music tools compose melodies, harmonies and complete songs from a text description or a short " +
                "musical idea, helping musicians sketch film scores, explore new genres and produce royalty-free " +
                "background music.",
            ReadMoreUrl: "https://en.wikipedia.org/wiki/Music_and_artificial_intelligence",
            TextFirst: false,
            Tools: new List<CreativeTool>
            {
                new("Suno", "~/img/tools/suno.jpg",
                    "Suno is a generative AI music program that creates realistic songs combining vocals and " +
                    "instrumentation from a short text prompt. It has been widely available since December 2023.",
                    "Wikipedia", "https://suno.com/"),
                new("AIVA", "~/img/tools/aiva.jpg",
                    "AIVA (Artificial Intelligence Virtual Artist) is an AI composer created in 2016 that writes " +
                    "classical and symphonic soundtracks, and is recognised by the music rights society SACEM.",
                    "Wikipedia", "https://www.aiva.ai/"),
            }),
        new(
            Id: "ai-creative-writing",
            Title: "AI Creative Writing",
            Description:
                "AI writing assistants use large language models to help authors brainstorm plots, draft poems, " +
                "scripts and short stories, and revise their work for style and tone, acting as a creative " +
                "partner for human writers.",
            ReadMoreUrl: "https://en.wikipedia.org/wiki/Large_language_model",
            TextFirst: true,
            Tools: new List<CreativeTool>
            {
                new("Sudowrite", "~/img/tools/sudowrite.jpg",
                    "Sudowrite is an AI writing partner built for fiction authors, helping novelists brainstorm " +
                    "ideas, describe scenes and expand their drafts while keeping their own voice.",
                    "Sudowrite", "https://www.sudowrite.com/"),
                new("NovelAI", "~/img/tools/novelai.jpg",
                    "NovelAI is a subscription service for AI-assisted storytelling and text-to-image synthesis, " +
                    "first launched in beta in June 2021.",
                    "Wikipedia", "https://novelai.net/"),
            }),
    };

    /// <summary>Every external text source used on the site, credited on the Contact page.</summary>
    public static IReadOnlyList<(string Title, string Url)> TextSources { get; } = new List<(string, string)>
    {
        ("Wikipedia – Artificial intelligence art", "https://en.wikipedia.org/wiki/Artificial_intelligence_art"),
        ("Wikipedia – Text-to-image model", "https://en.wikipedia.org/wiki/Text-to-image_model"),
        ("Wikipedia – Midjourney", "https://en.wikipedia.org/wiki/Midjourney"),
        ("Wikipedia – Adobe Firefly", "https://en.wikipedia.org/wiki/Adobe_Firefly"),
        ("Wikipedia – Music and artificial intelligence", "https://en.wikipedia.org/wiki/Music_and_artificial_intelligence"),
        ("Wikipedia – Suno AI", "https://en.wikipedia.org/wiki/Suno_AI"),
        ("Wikipedia – AIVA", "https://en.wikipedia.org/wiki/AIVA"),
        ("Wikipedia – Large language model", "https://en.wikipedia.org/wiki/Large_language_model"),
        ("Wikipedia – NovelAI", "https://en.wikipedia.org/wiki/NovelAI"),
        ("Sudowrite – official website", "https://www.sudowrite.com/"),
        ("Wikipedia – AARON", "https://en.wikipedia.org/wiki/AARON"),
        ("Wikipedia – Experiments in Musical Intelligence", "https://en.wikipedia.org/wiki/Experiments_in_Musical_Intelligence"),
        ("Wikipedia – DeepDream", "https://en.wikipedia.org/wiki/DeepDream"),
        ("Wikipedia – Edmond de Belamy", "https://en.wikipedia.org/wiki/Edmond_de_Belamy"),
        ("Wikipedia – DALL-E", "https://en.wikipedia.org/wiki/DALL-E"),
        ("Wikipedia – Stable Diffusion", "https://en.wikipedia.org/wiki/Stable_Diffusion"),
        ("Wikipedia – Théâtre D'opéra Spatial", "https://en.wikipedia.org/wiki/Th%C3%A9%C3%A2tre_D%27op%C3%A9ra_Spatial"),
        ("Wikipedia – Sora (text-to-video model)", "https://en.wikipedia.org/wiki/Sora_(text-to-video_model)"),
    };
}
