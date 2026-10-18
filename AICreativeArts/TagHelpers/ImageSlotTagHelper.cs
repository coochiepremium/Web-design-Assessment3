using System.Text.Encodings.Web;
using AICreativeArts.Content;
using AICreativeArts.Services;
using Microsoft.AspNetCore.Mvc.Razor.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AICreativeArts.TagHelpers;

/// <summary>
/// Renders an image slot from <see cref="ImageLibrary"/>.
/// Usage: &lt;image-slot key="tile-laion" class="tool-tile-image" /&gt;
/// If the file exists in wwwroot it becomes an &lt;img&gt; (with a cache-busting version);
/// otherwise a placeholder describes the image to add and where to save it.
/// </summary>
[HtmlTargetElement("image-slot", Attributes = "key", TagStructure = TagStructure.WithoutEndTag)]
public class ImageSlotTagHelper : TagHelper
{
    private readonly IMediaLibrary _media;
    private readonly IFileVersionProvider _versions;

    public ImageSlotTagHelper(IMediaLibrary media, IFileVersionProvider versions)
    {
        _media = media;
        _versions = versions;
    }

    public string Key { get; set; } = "";

    /// <summary>Extra CSS classes for the image or placeholder.</summary>
    [HtmlAttributeName("class")]
    public string? CssClass { get; set; }

    /// <summary>Whether the image may load lazily (default true).</summary>
    public bool Lazy { get; set; } = true;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var slot = _media.Find(Key);
        if (slot is null)
        {
            output.SuppressOutput();
            return;
        }

        if (_media.HasFile(slot))
        {
            var request = ViewContext.HttpContext.Request;
            output.TagName = "img";
            output.TagMode = TagMode.SelfClosing;
            output.Attributes.SetAttribute("src", _versions.AddFileVersionToPath(request.PathBase, $"{request.PathBase}/{slot.File}"));
            output.Attributes.SetAttribute("alt", slot.Alt);
            output.Attributes.SetAttribute("decoding", "async");
            if (Lazy)
            {
                output.Attributes.SetAttribute("loading", "lazy");
            }

            SetClass(output, CssClass);
            return;
        }

        var encoder = HtmlEncoder.Default;
        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("role", "img");
        output.Attributes.SetAttribute("aria-label", $"Image placeholder: {slot.Alt}");
        output.Attributes.SetAttribute("data-image-slot", slot.Key);
        SetClass(output, $"image-slot {CssClass}");
        output.Content.SetHtmlContent(
            "<span class=\"image-slot-badge\"><i class=\"fa-solid fa-image\" aria-hidden=\"true\"></i> Image needed</span>" +
            $"<span class=\"image-slot-text\">{encoder.Encode(slot.Description)}</span>" +
            $"<code class=\"image-slot-path\">wwwroot/{encoder.Encode(slot.File)}</code>" +
            $"<span class=\"image-slot-size\">{encoder.Encode(slot.Recommended)}</span>");
    }

    private static void SetClass(TagHelperOutput output, string? classes)
    {
        var value = string.Join(' ', (classes ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (value.Length > 0)
        {
            output.Attributes.SetAttribute("class", value);
        }
    }
}

/// <summary>
/// Shows a small note when a CSS background image slot has no file yet.
/// Usage: &lt;background-slot-note key="hero-background" /&gt;. Renders nothing once the file exists.
/// </summary>
[HtmlTargetElement("background-slot-note", Attributes = "key", TagStructure = TagStructure.WithoutEndTag)]
public class BackgroundSlotNoteTagHelper : TagHelper
{
    private readonly IMediaLibrary _media;

    public BackgroundSlotNoteTagHelper(IMediaLibrary media) => _media = media;

    public string Key { get; set; } = "";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var slot = _media.Find(Key);
        if (slot is null || _media.HasFile(slot))
        {
            output.SuppressOutput();
            return;
        }

        var encoder = HtmlEncoder.Default;
        output.TagName = "p";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "slot-note");
        output.Content.SetHtmlContent(
            "<i class=\"fa-solid fa-image\" aria-hidden=\"true\"></i> " +
            $"<strong>Background image needed:</strong> {encoder.Encode(slot.Description)} " +
            $"Save it as <code>wwwroot/{encoder.Encode(slot.File)}</code> ({encoder.Encode(slot.Recommended)}).");
    }
}
