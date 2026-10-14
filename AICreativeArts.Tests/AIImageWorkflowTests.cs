using System.Net;
using System.Net.Http.Headers;
using AICreativeArts.Data;
using AICreativeArts.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AICreativeArts.Tests;

/// <summary>Likes, uploads, edits and deletes, exercised through real HTTP requests.</summary>
public class AIImageWorkflowTests : IClassFixture<MuseAIFactory>
{
    // A valid 1x1 PNG.
    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+ip1sAAAAASUVORK5CYII=");

    private readonly MuseAIFactory _factory;

    public AIImageWorkflowTests(MuseAIFactory factory) => _factory = factory;

    [Fact]
    public async Task Member_can_like_an_image_once_per_login()
    {
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);
        var before = await browser.GetLikeCountAsync(4);

        var first = await browser.PostFormAsync("/AIImages/Like/4", "/AIImages");
        Assert.Equal(HttpStatusCode.Redirect, first.StatusCode);
        Assert.Equal("/AIImages#image-4", first.Headers.Location!.OriginalString);
        Assert.Equal(before + 1, await browser.GetLikeCountAsync(4));

        await browser.PostFormAsync("/AIImages/Like/4", "/AIImages");
        Assert.Equal(before + 1, await browser.GetLikeCountAsync(4));

        // Logging out and back in allows one more like.
        await browser.LogoutAsync();
        await browser.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);
        await browser.PostFormAsync("/AIImages/Like/4", "/AIImages");
        Assert.Equal(before + 2, await browser.GetLikeCountAsync(4));
    }

    [Fact]
    public async Task Visitor_cannot_like()
    {
        var browser = _factory.CreateBrowser();

        var response = await browser.PostFormAsync("/AIImages/Like/1", "/Identity/Account/Login");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Identity/Account/Login", response.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_rejected()
    {
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);

        var response = await browser.PostAsync("/AIImages/Like/1", new FormUrlEncodedContent(new Dictionary<string, string>()));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_is_sorted_by_likes_descending()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var expected = await db.AIImages.OrderByDescending(i => i.Like).ThenByDescending(i => i.UploadDate)
            .Select(i => i.Id).ToListAsync();

        var html = await _factory.CreateBrowser().GetStringAsync("/AIImages");
        var actual = System.Text.RegularExpressions.Regex.Matches(html, "<article id=\"image-(\\d+)\"")
            .Select(m => int.Parse(m.Groups[1].Value)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Member_upload_is_saved_with_server_controlled_values()
    {
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);
        var token = await browser.GetAntiforgeryTokenAsync("/AIImages/Create");

        var form = CreateUploadForm(token, "A lighthouse on a stormy cliff, oil painting", "Unit Test Generator",
            "lighthouse.png", TinyPng);
        form.Add(new StringContent("999"), "Like");
        form.Add(new StringContent("2000-01-01T00:00"), "UploadDate");
        var response = await browser.PostAsync("/AIImages/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/#upload-ai-images", response.Headers.Location!.OriginalString);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var saved = await db.AIImages.SingleAsync(i => i.ImageGenerator == "Unit Test Generator");
        Assert.Equal(0, saved.Like);
        Assert.True(saved.UploadDate > DateTime.Now.AddMinutes(-5));
        Assert.StartsWith("lighthouse-", saved.Filename);
    }

    [Fact]
    public async Task Upload_with_fake_image_shows_a_validation_message()
    {
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);
        var token = await browser.GetAntiforgeryTokenAsync("/AIImages/Create");

        var form = CreateUploadForm(token, "Not really an image", "Fake", "photo.jpg", "plain text"u8.ToArray());
        var response = await browser.PostAsync("/AIImages/Create", form);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("not a valid image", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_can_edit_and_delete()
    {
        var admin = _factory.CreateBrowser();
        await admin.LoginAsync(BrowserExtensions.AdminEmail, BrowserExtensions.AdminPassword);

        var token = await admin.GetAntiforgeryTokenAsync("/AIImages/Create");
        await admin.PostAsync("/AIImages/Create", CreateUploadForm(token, "Temporary image for editing", "Before Edit", "temp.png", TinyPng));

        int id;
        using (var scope = _factory.Services.CreateScope())
        {
            id = await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>()
                .AIImages.Where(i => i.ImageGenerator == "Before Edit").Select(i => i.Id).SingleAsync();
        }

        token = await admin.GetAntiforgeryTokenAsync($"/AIImages/Edit/{id}");
        var edit = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(id.ToString()), "Id" },
            { new StringContent("Temporary image after editing"), "Prompt" },
            { new StringContent("After Edit"), "ImageGenerator" },
        };
        var edited = await admin.PostAsync($"/AIImages/Edit/{id}", edit);
        Assert.Equal(HttpStatusCode.Redirect, edited.StatusCode);
        Assert.Equal($"/AIImages#image-{id}", edited.Headers.Location!.OriginalString);

        var deleted = await admin.PostFormAsync($"/AIImages/Delete/{id}", $"/AIImages/Delete/{id}");
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);

        using var check = _factory.Services.CreateScope();
        Assert.False(await check.ServiceProvider.GetRequiredService<ApplicationDbContext>().AIImages.AnyAsync(i => i.Id == id));
    }

    private static MultipartFormDataContent CreateUploadForm(string token, string prompt, string generator,
        string fileName, byte[] bytes)
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(fileName.EndsWith(".png") ? "image/png" : "image/jpeg");
        return new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent(prompt), "Prompt" },
            { new StringContent(generator), "ImageGenerator" },
            { file, "ImageFile", fileName },
        };
    }
}
