using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AICreativeArts.Infrastructure;

public static class SiteUrlExtensions
{
    /// <summary>Public URL of an uploaded image, e.g. /uploads/golden-canary-3f9a2c1b.jpg.</summary>
    public static string UploadedImage(this IUrlHelper url, string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return "";
        }

        var folder = url.ActionContext.HttpContext.RequestServices
            .GetRequiredService<IOptions<UploadOptions>>().Value.FolderName;
        return url.Content($"~/{folder}/{Uri.EscapeDataString(filename)}");
    }

    /// <summary>Link to an image block on the AI Images page, e.g. /AIImages#image-3.</summary>
    public static string ImageAnchor(this IUrlHelper url, int id) =>
        url.Action("Index", "AIImages", values: null, protocol: null, host: null, fragment: AnchorId(id))!;

    public static string AnchorId(int id) => $"image-{id}";
}
