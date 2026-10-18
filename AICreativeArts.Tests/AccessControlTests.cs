using System.Net;
using AICreativeArts.Tests.Infrastructure;
using Xunit;

namespace AICreativeArts.Tests;

/// <summary>Checks the visitor / member / admin rules from the assessment brief.</summary>
public class AccessControlTests : IClassFixture<SiteFactory>
{
    private readonly SiteFactory _factory;

    public AccessControlTests(SiteFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Contact")]
    [InlineData("/Home/ArtistShield")]
    [InlineData("/AIImages")]
    [InlineData("/Identity/Account/Login")]
    [InlineData("/Identity/Account/Register")]
    public async Task Visitor_can_open_public_pages(string url)
    {
        var response = await _factory.CreateBrowser().GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/AIImages/Create")]
    [InlineData("/AIImages/Details/1")]
    [InlineData("/AIImages/Edit/1")]
    [InlineData("/AIImages/Delete/1")]
    public async Task Visitor_is_sent_to_login_for_protected_pages(string url)
    {
        var response = await _factory.CreateBrowser().GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Identity/Account/Login", response.Headers.Location!.PathAndQuery);
        Assert.Contains("ReturnUrl=" + Uri.EscapeDataString(url), response.Headers.Location.Query);
    }

    [Fact]
    public async Task Member_can_open_create_page()
    {
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);

        var response = await browser.GetAsync("/AIImages/Create");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/AIImages/Details/1")]
    [InlineData("/AIImages/Edit/1")]
    [InlineData("/AIImages/Delete/1")]
    public async Task Member_is_denied_admin_pages(string url)
    {
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);

        var response = await browser.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/Identity/Account/AccessDenied", response.Headers.Location!.PathAndQuery);
    }

    [Theory]
    [InlineData("/AIImages/Create")]
    [InlineData("/AIImages/Details/{id}")]
    [InlineData("/AIImages/Edit/{id}")]
    [InlineData("/AIImages/Delete/{id}")]
    public async Task Admin_can_open_every_page(string url)
    {
        var id = await _factory.AddImageAsync("Admin Page Check");
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(BrowserExtensions.AdminEmail, BrowserExtensions.AdminPassword);

        var response = await browser.GetAsync(url.Replace("{id}", id.ToString()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Buttons_match_the_viewer_role()
    {
        var id = await _factory.AddImageAsync("Button Check");
        var visitorHtml = await _factory.CreateBrowser().GetStringAsync("/AIImages");
        Assert.DoesNotContain("like-form", visitorHtml);

        var member = _factory.CreateBrowser();
        await member.LoginAsync(BrowserExtensions.MemberEmail, BrowserExtensions.MemberPassword);
        var memberHtml = await member.GetStringAsync("/AIImages");
        Assert.Contains("like-form", memberHtml);
        Assert.DoesNotContain("/AIImages/Edit/", memberHtml);
        Assert.DoesNotContain("/AIImages/Delete/", memberHtml);

        var admin = _factory.CreateBrowser();
        await admin.LoginAsync(BrowserExtensions.AdminEmail, BrowserExtensions.AdminPassword);
        var adminHtml = await admin.GetStringAsync("/AIImages");
        Assert.Contains("like-form", adminHtml);
        Assert.Contains($"/AIImages/Edit/{id}", adminHtml);
        Assert.Contains($"/AIImages/Delete/{id}", adminHtml);
    }

    [Theory]
    [InlineData("/AIImages/Details/999")]
    [InlineData("/AIImages/Edit/abc")]
    [InlineData("/AIImages/Delete/2147483648")]
    [InlineData("/this-page-does-not-exist")]
    public async Task Unknown_pages_show_a_friendly_404(string url)
    {
        var browser = _factory.CreateBrowser();
        await browser.LoginAsync(BrowserExtensions.AdminEmail, BrowserExtensions.AdminPassword);

        var response = await browser.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page Not Found", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_ignores_external_return_urls()
    {
        var browser = _factory.CreateBrowser();
        var token = await browser.GetAntiforgeryTokenAsync("/Identity/Account/Login");

        var response = await browser.PostAsync("/Identity/Account/Login?returnUrl=https%3A%2F%2Fevil.example", new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["Input.Email"] = BrowserExtensions.MemberEmail,
                ["Input.Password"] = BrowserExtensions.MemberPassword,
                ["__RequestVerificationToken"] = token,
            }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location!.OriginalString);
    }
}
