namespace AICreativeArts.Content;

/// <summary>
/// A home page block ("case file"): a text card plus two square example tiles.
/// <see cref="TextFirst"/> controls the side the card sits on (and the order on phones).
/// </summary>
public record FeatureSection(
    string Id,
    string CaseLabel,
    string Title,
    string Description,
    string KeyFact,
    string ReadMoreUrl,
    bool TextFirst,
    IReadOnlyList<CaseExample> Examples);

/// <summary>
/// A square tile: its image comes from the image slot <see cref="SlotKey"/>, the text
/// appears on mouse over, and a click opens <see cref="Url"/> in a new tab.
/// </summary>
public record CaseExample(
    string Name,
    string SlotKey,
    string HoverText,
    string Source,
    string Url);
