using AICreativeArts.Data;
using AICreativeArts.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AICreativeArts.Tests.Infrastructure;

/// <summary>
/// Hosts the real site in memory with its own throw-away SQLite database and upload
/// folder, so tests never touch the developer's app.db or wwwroot/uploads.
/// </summary>
public class MuseAIFactory : WebApplicationFactory<Program>
{
    public const string UploadFolder = "uploads-tests";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"museai-tests-{Guid.NewGuid():N}.db");
    private string? _uploadPath;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices((context, services) =>
        {
            _uploadPath = Path.Combine(context.HostingEnvironment.ContentRootPath, "wwwroot", UploadFolder);

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"DataSource={_databasePath}"));
            services.PostConfigure<UploadOptions>(options => options.FolderName = UploadFolder);
        });
    }

    /// <summary>A client that keeps cookies (like a browser) but does not follow redirects.</summary>
    public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        TryDelete(() => File.Delete(_databasePath));
        if (_uploadPath is not null && Directory.Exists(_uploadPath))
        {
            TryDelete(() => Directory.Delete(_uploadPath, recursive: true));
        }
    }

    private static void TryDelete(Action delete)
    {
        try
        {
            delete();
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
