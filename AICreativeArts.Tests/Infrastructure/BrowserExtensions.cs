using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace AICreativeArts.Tests.Infrastructure;

public static partial class BrowserExtensions
{
    public const string AdminEmail = "admin@asp.net";
    public const string AdminPassword = "Admin@123";
    public const string MemberEmail = "member@asp.net";
    public const string MemberPassword = "Member@123";

    /// <summary>Reads the antiforgery token from a page, exactly as a browser form would submit it.</summary>
    public static async Task<string> GetAntiforgeryTokenAsync(this HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url);
        var match = TokenPattern().Match(html);
        Assert.True(match.Success, $"No antiforgery token found on {url}");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public static async Task LoginAsync(this HttpClient client, string email, string password)
    {
        var token = await client.GetAntiforgeryTokenAsync("/Identity/Account/Login");
        var response = await client.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    public static async Task LogoutAsync(this HttpClient client)
    {
        var token = await client.GetAntiforgeryTokenAsync("/");
        var response = await client.PostAsync("/Identity/Account/Logout?returnUrl=%2F", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient client, string url, string tokenPage,
        Dictionary<string, string>? fields = null)
    {
        var values = new Dictionary<string, string>(fields ?? new Dictionary<string, string>())
        {
            ["__RequestVerificationToken"] = await client.GetAntiforgeryTokenAsync(tokenPage),
        };
        return await client.PostAsync(url, new FormUrlEncodedContent(values));
    }

    /// <summary>The "Like (n)" count shown for an image on the AI Images page.</summary>
    public static async Task<int> GetLikeCountAsync(this HttpClient client, int imageId)
    {
        var html = await client.GetStringAsync("/AIImages");
        var article = Regex.Match(html, $"<article id=\"image-{imageId}\"[\\s\\S]*?</article>").Value;
        var count = Regex.Match(article, @"Like[ds]? \((\d+)\)");
        Assert.True(count.Success, $"No like count found for image {imageId}");
        return int.Parse(count.Groups[1].Value);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}
