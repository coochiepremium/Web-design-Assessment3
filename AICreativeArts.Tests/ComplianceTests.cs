using System.Text.RegularExpressions;
using Xunit;

namespace AICreativeArts.Tests;

/// <summary>
/// Guards the brief's rules: all CSS in external stylesheets and all JavaScript in
/// site.js, so no view may contain inline styles, internal style blocks, internal
/// scripts or inline event handlers.
/// </summary>
public partial class ComplianceTests
{
    private static readonly string ProjectDirectory = FindProjectDirectory();

    public static IEnumerable<object[]> Views() =>
        Directory.EnumerateFiles(ProjectDirectory, "*.cshtml", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                        && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(path => new object[] { Path.GetRelativePath(ProjectDirectory, path) });

    [Theory]
    [MemberData(nameof(Views))]
    public void View_has_no_inline_css_or_javascript(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine(ProjectDirectory, relativePath));

        Assert.DoesNotMatch(InlineStyleAttribute(), source);
        Assert.DoesNotMatch(StyleElement(), source);
        Assert.DoesNotMatch(ScriptWithoutSource(), source);
        Assert.DoesNotMatch(InlineEventHandler(), source);
    }

    [Fact]
    public void Layout_uses_external_site_css_and_site_js()
    {
        var layout = File.ReadAllText(Path.Combine(ProjectDirectory, "Views", "Shared", "_Layout.cshtml"));

        Assert.Contains("~/css/site.css", layout);
        Assert.Contains("~/js/site.js", layout);
        Assert.True(File.Exists(Path.Combine(ProjectDirectory, "wwwroot", "css", "site.css")));
        Assert.True(File.Exists(Path.Combine(ProjectDirectory, "wwwroot", "js", "site.js")));
    }

    [Fact]
    public void Menu_and_footer_link_to_every_main_page()
    {
        var layout = File.ReadAllText(Path.Combine(ProjectDirectory, "Views", "Shared", "_Layout.cshtml"));
        var header = layout[..layout.IndexOf("<main", StringComparison.Ordinal)];
        var footer = layout[layout.IndexOf("<footer", StringComparison.Ordinal)..];

        foreach (var part in new[] { header, footer })
        {
            Assert.Contains("asp-controller=\"Home\" asp-action=\"Index\"", part);
            Assert.Contains("asp-controller=\"AIImages\" asp-action=\"Index\"", part);
            Assert.Contains("asp-controller=\"Home\" asp-action=\"Contact\"", part);
            Assert.Contains("asp-controller=\"Home\" asp-action=\"PromptLab\"", part);
        }
    }

    private static string FindProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "AICreativeArts", "AICreativeArts.csproj");
            if (File.Exists(candidate))
            {
                return Path.GetDirectoryName(candidate)!;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the AICreativeArts project folder.");
    }

    [GeneratedRegex(@"\sstyle\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex InlineStyleAttribute();

    [GeneratedRegex(@"<style[\s>]", RegexOptions.IgnoreCase)]
    private static partial Regex StyleElement();

    [GeneratedRegex(@"<script(?![^>]*\bsrc\s*=)[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex ScriptWithoutSource();

    [GeneratedRegex(@"\son(click|load|error|change|submit|mouseover|mouseout|focus|blur|input|key\w+)\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex InlineEventHandler();
}
