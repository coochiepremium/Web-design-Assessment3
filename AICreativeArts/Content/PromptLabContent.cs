namespace AICreativeArts.Content;

/// <summary>A clickable option in the Prompt Lab ("Watercolour" adds "loose watercolour illustration").</summary>
public record PromptOption(string Label, string Value);

public record PromptGroup(string Key, string Title, string Icon, IReadOnlyList<PromptOption> Options);

public record TimelineEntry(string Year, string Title, string Text, string SourceUrl);

/// <summary>Data for the Prompt Lab page (the additional creative page).</summary>
public static class PromptLabContent
{
    public static IReadOnlyList<PromptGroup> Groups { get; } = new List<PromptGroup>
    {
        new("subject", "Subject", "fa-mountain-sun", new List<PromptOption>
        {
            new("Stormy lighthouse", "a lighthouse on a stormy cliff"),
            new("Reading fox", "a fox reading a book by candlelight"),
            new("Floating city", "a floating city above the clouds"),
            new("Jazz trio", "a jazz trio playing in the rain"),
            new("Space gardener", "an astronaut tending a rooftop garden"),
            new("Koi pond", "a koi pond in autumn"),
        }),
        new("style", "Art style", "fa-palette", new List<PromptOption>
        {
            new("Oil painting", "textured oil painting"),
            new("Watercolour", "loose watercolour illustration"),
            new("Ukiyo-e", "ukiyo-e woodblock print"),
            new("Art nouveau", "art nouveau poster"),
            new("Pixel art", "16-bit pixel art"),
            new("Surrealism", "surrealist painting"),
            new("Paper cut", "layered paper-cut collage"),
            new("Photograph", "cinematic photograph"),
        }),
        new("lighting", "Lighting", "fa-lightbulb", new List<PromptOption>
        {
            new("Golden hour", "golden hour light"),
            new("Neon glow", "glowing neon light"),
            new("Candlelit", "soft candlelight"),
            new("Moonlight", "cool moonlight"),
            new("Studio", "bright studio lighting"),
            new("Light rays", "dramatic volumetric light rays"),
        }),
        new("palette", "Colour palette", "fa-droplet", new List<PromptOption>
        {
            new("Warm sunset", "warm sunset colour palette"),
            new("Pastel", "soft pastel colour palette"),
            new("Monochrome", "black and white palette"),
            new("Jewel tones", "rich jewel-tone colours"),
            new("Earthy", "earthy natural tones"),
            new("Vaporwave", "pink and cyan vaporwave colours"),
        }),
        new("mood", "Mood", "fa-face-smile", new List<PromptOption>
        {
            new("Dreamy", "dreamy mood"),
            new("Mysterious", "mysterious mood"),
            new("Joyful", "joyful mood"),
            new("Melancholic", "melancholic mood"),
            new("Epic", "epic mood"),
            new("Peaceful", "peaceful mood"),
        }),
        new("framing", "Framing", "fa-crop-simple", new List<PromptOption>
        {
            new("Close-up", "close-up view"),
            new("Wide shot", "wide establishing shot"),
            new("Bird's-eye", "bird's-eye view"),
            new("Isometric", "isometric view"),
            new("Symmetrical", "symmetrical composition"),
            new("Rule of thirds", "rule-of-thirds composition"),
        }),
    };

    public static IReadOnlyList<TimelineEntry> Timeline { get; } = new List<TimelineEntry>
    {
        new("1970s", "AARON",
            "Artist Harold Cohen begins developing AARON, one of the first computer programs to create original drawings on its own.",
            "https://en.wikipedia.org/wiki/AARON"),
        new("1981", "Experiments in Musical Intelligence",
            "Composer David Cope starts EMI, software that analyses existing music and writes new pieces in the style of composers such as Bach.",
            "https://en.wikipedia.org/wiki/Experiments_in_Musical_Intelligence"),
        new("2015", "DeepDream",
            "Google's DeepDream turns the patterns a neural network has learned into swirling, dream-like images, bringing AI art to a wide audience.",
            "https://en.wikipedia.org/wiki/DeepDream"),
        new("2018", "Edmond de Belamy",
            "A portrait created with a generative adversarial network by the Paris collective Obvious sells at Christie's for US$432,500.",
            "https://en.wikipedia.org/wiki/Edmond_de_Belamy"),
        new("2021", "DALL-E",
            "OpenAI announces DALL-E, a model that creates images from text descriptions and popularises the idea of the prompt.",
            "https://en.wikipedia.org/wiki/DALL-E"),
        new("2022", "Stable Diffusion & Midjourney",
            "Stable Diffusion is released openly, and an image made with Midjourney wins a digital art prize at the Colorado State Fair.",
            "https://en.wikipedia.org/wiki/Th%C3%A9%C3%A2tre_D%27op%C3%A9ra_Spatial"),
        new("2023", "Firefly & AI songs",
            "Adobe launches Firefly inside its creative apps, and tools such as Suno let anyone generate a complete song from a text prompt.",
            "https://en.wikipedia.org/wiki/Adobe_Firefly"),
        new("2024", "Text-to-video",
            "OpenAI previews Sora, a model that generates short, detailed video clips from text, extending generative AI into film-making.",
            "https://en.wikipedia.org/wiki/Sora_(text-to-video_model)"),
    };
}
