using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using AICreativeArts.Content;
using AICreativeArts.Infrastructure;
using AICreativeArts.Services;
using AICreativeArts.TagHelpers;
using AICreativeArts.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Data.Sqlite;
using Xunit;

namespace AICreativeArts.Tests;

/// <summary>
/// The site ships without images: every image is a slot that shows a descriptive
/// placeholder until the owner adds the file, and every slot needs a credit.
/// </summary>
public class ImageSlotTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public ImageSlotTests(SiteFactory factory) => _factory = factory;

    [Fact]
    public void Every_slot_is_described_and_unique()
    {
        Assert.Equal(ImageLibrary.Slots.Count, ImageLibrary.Slots.Select(s => s.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        foreach (var slot in ImageLibrary.Slots)
        {
            Assert.False(string.IsNullOrWhiteSpace(slot.Description), slot.Key);
            Assert.False(string.IsNullOrWhiteSpace(slot.Alt), slot.Key);
            Assert.False(string.IsNullOrWhiteSpace(slot.Recommended), slot.Key);
            Assert.False(slot.File.StartsWith('/'), $"{slot.Key} path must be relative to wwwroot");
        }
    }

    [Fact]
    public void Every_case_tile_uses_a_known_slot()
    {
        foreach (var example in SiteContent.Sections.SelectMany(s => s.Examples))
        {
            Assert.NotNull(ImageLibrary.Find(example.SlotKey));
        }
    }

    [Fact]
    public void Appsettings_has_a_credit_entry_for_every_slot()
    {
        var path = Path.Combine(ProjectPaths.WebProject, "appsettings.json");
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        var credits = json.RootElement.GetProperty("ImageCredits");

        foreach (var slot in ImageLibrary.Slots)
        {
            Assert.True(credits.TryGetProperty(slot.Key, out var entry), $"ImageCredits:{slot.Key} is missing from appsettings.json");
            foreach (var field in new[] { "Creator", "Source", "Url", "Licence" })
            {
                Assert.True(entry.TryGetProperty(field, out _), $"ImageCredits:{slot.Key}:{field} is missing");
            }
        }
    }

    [Fact]
    public void Tag_helper_renders_a_placeholder_when_the_file_is_missing()
    {
        var html = Render(hasFile: false, key: "tile-laion");

        Assert.StartsWith("<span", html);
        Assert.Contains("data-image-slot=\"tile-laion\"", html);
        Assert.Contains("Image needed", html);
        Assert.Contains("wwwroot/img/tiles/laion-5b.jpg", html);
        Assert.Contains("class=\"image-slot extra\"", html);
    }

    [Fact]
    public void Tag_helper_renders_an_image_when_the_file_exists()
    {
        var html = Render(hasFile: true, key: "tile-laion");

        Assert.StartsWith("<img", html);
        Assert.Contains("src=\"/img/tiles/laion-5b.jpg?v=test\"", html);
        Assert.Contains("alt=\"Mosaic representing billions of scraped images\"", html);
        Assert.Contains("loading=\"lazy\"", html);
    }

    [Fact]
    public void Tag_helper_renders_nothing_for_an_unknown_key()
    {
        Assert.Equal("", Render(hasFile: true, key: "does-not-exist"));
    }

    [Fact]
    public async Task Home_page_shows_a_placeholder_for_each_missing_tile_and_background()
    {
        var html = await _factory.CreateBrowser().GetStringAsync("/");

        foreach (var example in SiteContent.Sections.SelectMany(s => s.Examples))
        {
            Assert.Contains($"data-image-slot=\"{example.SlotKey}\"", html);
        }

        Assert.Contains("wwwroot/img/backgrounds/hero.jpg", html);
        Assert.Contains("wwwroot/img/backgrounds/section.jpg", html);
        Assert.DoesNotContain("has-hero-image", html);
    }

    [Fact]
    public async Task Contact_page_asks_for_a_reference_for_every_uncredited_image()
    {
        var html = await _factory.CreateBrowser().GetStringAsync("/Home/Contact");

        Assert.Equal(ImageLibrary.Slots.Count, Regex.Matches(html, "Reference needed").Count);
        foreach (var slot in ImageLibrary.Slots)
        {
            Assert.Contains($"ImageCredits:{slot.Key}", html);
        }
    }

    [Fact]
    public async Task Contact_page_shows_a_configured_credit()
    {
        using var factory = new SiteFactory("Testing", null, new Dictionary<string, string?>
        {
            ["ImageCredits:tile-laion:Creator"] = "Jane Doe",
            ["ImageCredits:tile-laion:Source"] = "Unsplash",
            ["ImageCredits:tile-laion:Url"] = "https://unsplash.com/photos/example",
            ["ImageCredits:tile-laion:Licence"] = "Unsplash Licence",
        });

        var html = await factory.CreateBrowser().GetStringAsync("/Home/Contact");

        Assert.Contains("Jane Doe, Unsplash (Unsplash Licence)", html);
        Assert.Contains("href=\"https://unsplash.com/photos/example\"", html);
        Assert.Equal(ImageLibrary.Slots.Count - 1, Regex.Matches(html, "Reference needed").Count);
    }

    [Theory]
    [InlineData("/img/backgrounds/hero.jpg")]
    [InlineData("/uploads/missing-file.png")]
    [InlineData("/favicon.ico")]
    public async Task Missing_files_get_a_plain_404_not_an_html_page(string url)
    {
        var response = await _factory.CreateBrowser().GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("<html", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task An_outdated_development_database_is_backed_up_and_recreated()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aiunmasked-legacy-{Guid.NewGuid():N}.db");
        await using (var connection = new SqliteConnection($"DataSource={path}"))
        {
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText =
                "CREATE TABLE \"__EFMigrationsHistory\" (\"MigrationId\" TEXT PRIMARY KEY, \"ProductVersion\" TEXT NOT NULL);" +
                "INSERT INTO \"__EFMigrationsHistory\" VALUES ('20261014005809_AddAIImages', '8.0.31');";
            await command.ExecuteNonQueryAsync();
        }

        SqliteConnection.ClearAllPools();

        using var factory = new SiteFactory("Development", path, null);
        var response = await factory.CreateBrowser().GetAsync("/AIImages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".bak-*"));
    }

    private static string Render(bool hasFile, string key)
    {
        var helper = new ImageSlotTagHelper(new FakeMedia(hasFile), new FakeVersions())
        {
            Key = key,
            CssClass = "extra",
            ViewContext = new ViewContext { HttpContext = new DefaultHttpContext() },
        };
        var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), "test");
        var output = new TagHelperOutput("image-slot", new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        helper.Process(context, output);

        using var writer = new StringWriter();
        output.WriteTo(writer, HtmlEncoder.Default);
        return writer.ToString();
    }

    private sealed class FakeMedia : IMediaLibrary
    {
        private readonly bool _hasFile;

        public FakeMedia(bool hasFile) => _hasFile = hasFile;

        public ImageSlot? Find(string key) => ImageLibrary.Find(key);

        public bool HasFile(ImageSlot slot) => _hasFile;

        public bool HasFile(string key) => _hasFile;

        public ImageCredit CreditFor(ImageSlot slot) => new();

        public IReadOnlyList<ImageSlotStatus> All() =>
            ImageLibrary.Slots.Select(s => new ImageSlotStatus(s, _hasFile, new ImageCredit())).ToList();
    }

    private sealed class FakeVersions : IFileVersionProvider
    {
        public string AddFileVersionToPath(PathString requestPathBase, string path) => path + "?v=test";
    }
}
