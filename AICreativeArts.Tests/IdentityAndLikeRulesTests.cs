using System.Net;
using AICreativeArts.Data;
using AICreativeArts.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AICreativeArts.Tests;

/// <summary>
/// Built-in Identity pages never show an error page, and the "one like per image per
/// login" rule is enforced in the database.
/// </summary>
public class IdentityAndLikeRulesTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public IdentityAndLikeRulesTests(SiteFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/Identity/Account/LoginWith2fa")]
    [InlineData("/Identity/Account/LoginWithRecoveryCode")]
    public async Task Two_factor_pages_opened_directly_redirect_to_login(string url)
    {
        var response = await _factory.CreateBrowser().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Identity/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/Identity/Account/Manage/GenerateRecoveryCodes")]
    [InlineData("/Identity/Account/Manage/Disable2fa")]
    public async Task Two_factor_manage_pages_without_two_factor_redirect(string url)
    {
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);

        var response = await browser.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Identity/Account/Manage/TwoFactorAuthentication", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Plain_text_identity_errors_become_the_friendly_status_page()
    {
        var response = await _factory.CreateBrowser().GetAsync("/Identity/Account/ResetPassword");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Bad Request", await response.Content.ReadAsStringAsync());
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Different_users_each_add_one_like()
    {
        var id = await _factory.AddImageAsync("Two Users");
        var member = _factory.CreateBrowser();
        var admin = _factory.CreateBrowser();
        await member.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);
        await admin.LoginAsync(BrowserExtensions.AdminEmail, BrowserExtensions.AdminPassword);

        await member.PostFormAsync($"/AIImages/Like/{id}", "/AIImages");
        await admin.PostFormAsync($"/AIImages/Like/{id}", "/AIImages");
        await admin.PostFormAsync($"/AIImages/Like/{id}", "/AIImages");

        Assert.Equal(2, await admin.GetLikeCountAsync(id));
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, await db.ImageLikes.CountAsync(l => l.AIImageId == id));
    }

    [Fact]
    public async Task Like_is_remembered_by_the_sign_in_cookie_alone()
    {
        var id = await _factory.AddImageAsync("Remembered Like");
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });

        // Log in by hand and keep only the sign-in cookie.
        var loginPage = await client.GetAsync("/Identity/Account/Login");
        var antiforgery = CookiesFrom(loginPage);
        var login = await Send(client, HttpMethod.Post, "/Identity/Account/Login", antiforgery, new Dictionary<string, string>
        {
            ["Input.Email"] = BrowserExtensions.MemberEmail,
            ["Input.Password"] = BrowserExtensions.MemberPassword,
            ["__RequestVerificationToken"] = await TokenFrom(loginPage),
        });
        var auth = CookiesFrom(login).Split("; ").Single(c => c.StartsWith(".AIUnmasked.Auth="));

        // Like the image once.
        var list = await Send(client, HttpMethod.Get, "/AIImages", auth);
        var like = await Send(client, HttpMethod.Post, $"/AIImages/Like/{id}", $"{auth}; {CookiesFrom(list)}",
            new Dictionary<string, string> { ["__RequestVerificationToken"] = await TokenFrom(list) });
        Assert.Equal(HttpStatusCode.Redirect, like.StatusCode);

        // A brand-new request with nothing but the sign-in cookie still knows about the like:
        // the rule depends on the login, not on any other browser state.
        var again = await Send(client, HttpMethod.Get, "/AIImages", auth);
        Assert.Contains("Liked (1)", await again.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Get_on_like_after_logging_in_returns_to_the_image()
    {
        var id = await _factory.AddImageAsync("Like Return");
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);

        var response = await browser.GetAsync($"/AIImages/Like/{id}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/AIImages#image-{id}", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Deleting_an_image_removes_its_likes()
    {
        var id = await _factory.AddImageAsync("Cascade");
        var admin = _factory.CreateBrowser();
        await admin.LoginAsync(BrowserExtensions.AdminEmail, BrowserExtensions.AdminPassword);
        await admin.PostFormAsync($"/AIImages/Like/{id}", "/AIImages");

        await admin.PostFormAsync($"/AIImages/Delete/{id}", $"/AIImages/Delete/{id}");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.ImageLikes.AnyAsync(l => l.AIImageId == id));
    }

    private static string CookiesFrom(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? string.Join("; ", values.Select(v => v.Split(';')[0]))
            : "";

    private static async Task<string> TokenFrom(HttpResponseMessage response)
    {
        var html = await response.Content.ReadAsStringAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(match.Success, "No antiforgery token on the page");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, string cookies,
        Dictionary<string, string>? form = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (cookies.Length > 0)
        {
            request.Headers.Add("Cookie", cookies);
        }

        if (form is not null)
        {
            request.Content = new FormUrlEncodedContent(form);
        }

        return client.SendAsync(request);
    }
}
