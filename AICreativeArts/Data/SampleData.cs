using AICreativeArts.Models;

namespace AICreativeArts.Data;

/// <summary>
/// Starter gallery inserted by the initial migration so the site is not empty on
/// first run. The artworks are original procedural images made for this project
/// (see tools/generate_art.py), so each one names the algorithm that produced it.
/// </summary>
public static class SampleData
{
    /// <summary>Folder (under wwwroot) holding the original sample files.</summary>
    public const string SampleFolder = "img/samples";

    public static IReadOnlyList<AIImage> Images { get; } = new List<AIImage>
    {
        new()
        {
            Id = 1,
            Prompt = "Thousands of golden light particles drifting along an invisible current, tracing silky " +
                     "strands across a deep midnight-blue canvas, long-exposure style.",
            ImageGenerator = "Flow Field Generator",
            UploadDate = new DateTime(2026, 9, 1, 9, 30, 0),
            Filename = "sample-golden-currents.jpg",
            Like = 7,
        },
        new()
        {
            Id = 2,
            Prompt = "A jewel-toned stained-glass window built from seventy irregular cells with dark lead " +
                     "lines, warm light glowing from the upper left.",
            ImageGenerator = "Voronoi Mosaic Generator",
            UploadDate = new DateTime(2026, 9, 3, 14, 5, 0),
            Filename = "sample-stained-glass.jpg",
            Like = 5,
        },
        new()
        {
            Id = 3,
            Prompt = "A decaying pendulum drawing in neon cyan fading to magenta, looping ribbons spiralling " +
                     "towards the centre on a black background.",
            ImageGenerator = "Harmonograph Simulator",
            UploadDate = new DateTime(2026, 9, 6, 19, 45, 0),
            Filename = "sample-harmonograph.jpg",
            Like = 4,
        },
        new()
        {
            Id = 4,
            Prompt = "Three ripple sources overlapping on a still pond, rendered as a glowing heat map of " +
                     "indigo, magenta, orange and gold.",
            ImageGenerator = "Wave Interference Generator",
            UploadDate = new DateTime(2026, 9, 10, 11, 20, 0),
            Filename = "sample-interference.jpg",
            Like = 3,
        },
        new()
        {
            Id = 5,
            Prompt = "A playful circle-packing pattern of concentric dots in orange, lavender, teal and gold on " +
                     "warm cream paper, mid-century print style.",
            ImageGenerator = "Circle Packing Generator",
            UploadDate = new DateTime(2026, 9, 14, 16, 0, 0),
            Filename = "sample-circle-garden.jpg",
            Like = 2,
        },
        new()
        {
            Id = 6,
            Prompt = "Layered violet mountain ridges fading into the haze at dusk under a pale full moon, " +
                     "minimalist flat illustration.",
            ImageGenerator = "Procedural Landscape Generator",
            UploadDate = new DateTime(2026, 9, 18, 8, 15, 0),
            Filename = "sample-dusk-ridges.jpg",
            Like = 1,
        },
    };
}
