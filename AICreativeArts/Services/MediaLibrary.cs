using AICreativeArts.Content;
using AICreativeArts.Infrastructure;
using Microsoft.Extensions.Options;

namespace AICreativeArts.Services;

/// <summary>An image slot together with whether its file exists and its credit.</summary>
public record ImageSlotStatus(ImageSlot Slot, bool HasFile, ImageCredit Credit);

/// <summary>
/// Answers "has the site owner added this image yet?" and "where did it come from?" for
/// every slot in <see cref="ImageLibrary"/>. Views use it to show either the image or a
/// placeholder, and the Contact page uses it to list image credits.
/// </summary>
public interface IMediaLibrary
{
    ImageSlot? Find(string key);

    bool HasFile(ImageSlot slot);

    bool HasFile(string key);

    ImageCredit CreditFor(ImageSlot slot);

    IReadOnlyList<ImageSlotStatus> All();
}

public class MediaLibrary : IMediaLibrary
{
    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<ImageCreditOptions> _credits;

    public MediaLibrary(IWebHostEnvironment environment, IOptionsMonitor<ImageCreditOptions> credits)
    {
        _environment = environment;
        _credits = credits;
    }

    public ImageSlot? Find(string key) => ImageLibrary.Find(key);

    // Checked on every request (a cheap file-system lookup), so a newly added image
    // appears straight away without restarting the site.
    public bool HasFile(ImageSlot slot) => _environment.WebRootFileProvider.GetFileInfo(slot.File).Exists;

    public bool HasFile(string key) => Find(key) is { } slot && HasFile(slot);

    public ImageCredit CreditFor(ImageSlot slot) =>
        _credits.CurrentValue.TryGetValue(slot.Key, out var credit) && credit is not null ? credit : new ImageCredit();

    public IReadOnlyList<ImageSlotStatus> All() =>
        ImageLibrary.Slots.Select(slot => new ImageSlotStatus(slot, HasFile(slot), CreditFor(slot))).ToList();
}
