using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AICreativeArts.TagHelpers;

/// <summary>
/// Highlights the menu link for the current page.
/// Usage: &lt;a nav-active="Home/Index"&gt; or &lt;a nav-active="AIImages/*"&gt;.
/// Adds the "active" class and aria-current="page" for screen readers.
/// </summary>
[HtmlTargetElement("a", Attributes = AttributeName)]
public class NavActiveTagHelper : TagHelper
{
    private const string AttributeName = "nav-active";

    [HtmlAttributeName(AttributeName)]
    public string Match { get; set; } = "";

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var parts = Match.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        var values = ViewContext.RouteData.Values;
        var controller = values["controller"]?.ToString();
        var action = values["action"]?.ToString();

        var isActive = string.Equals(controller, parts[0], StringComparison.OrdinalIgnoreCase)
            && (parts.Length == 1 || parts[1] == "*" || string.Equals(action, parts[1], StringComparison.OrdinalIgnoreCase));

        if (isActive)
        {
            output.AddClass("active", HtmlEncoder.Default);
            output.Attributes.SetAttribute("aria-current", "page");
        }
    }
}
