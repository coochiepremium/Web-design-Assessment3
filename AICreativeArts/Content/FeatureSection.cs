namespace AICreativeArts.Content;

/// <summary>A home page block: a text card plus two square tool images.</summary>
public record FeatureSection(
    string Id,
    string Title,
    string Description,
    string ReadMoreUrl,
    bool TextFirst,
    IReadOnlyList<CreativeTool> Tools);

/// <summary>A square tool image with the text shown on mouse over and its external link.</summary>
public record CreativeTool(
    string Name,
    string ImagePath,
    string HoverText,
    string Source,
    string Url);
