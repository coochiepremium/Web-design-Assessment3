using AICreativeArts.Data;
using AICreativeArts.Infrastructure;
using AICreativeArts.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AICreativeArts.Tests.Infrastructure;

/// <summary>
/// Hosts the real site in memory with its own throw-away SQLite database and upload
/// folder, so tests never touch the developer's app.db or wwwroot/uploads.
/// </summary>
public class SiteFactory : WebApplicationFactory<Program>
{
    public const string UploadFolder = "uploads-tests";

    private readonly string _environment;
    private readonly Dictionary<string, string?> _settings;
    private string? _uploadPath;

    public SiteFactory()
        : this("Testing", null, null)
    {
    }

    /// <param name="environment">Hosting environment name (the old-database safeguard only runs in Development).</param>
    /// <param name="databasePath">SQLite file to use; a new temporary file when null.</param>
    /// <param name="settings">Extra configuration values, e.g. image credits.</param>
    internal SiteFactory(string environment, string? databasePath, Dictionary<string, string?>? settings)
    {
        _environment = environment;
        DatabasePath = databasePath ?? Path.Combine(Path.GetTempPath(), $"aiunmasked-tests-{Guid.NewGuid():N}.db");
        _settings = settings ?? new Dictionary<string, string?>();
    }

    public string DatabasePath { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(_environment);
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(_settings));
        builder.ConfigureServices((context, services) =>
        {
            _uploadPath = Path.Combine(context.HostingEnvironment.ContentRootPath, "wwwroot", UploadFolder);

            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"DataSource={DatabasePath}"));
            services.PostConfigure<UploadOptions>(options => options.FolderName = UploadFolder);
        });
    }

    /// <summary>A client that keeps cookies (like a browser) but does not follow redirects.</summary>
    public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    });

    /// <summary>Adds a gallery record straight to the database (the gallery starts empty).</summary>
    public async Task<int> AddImageAsync(string generator, int likes = 0, string? filename = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var image = new AIImage
        {
            Prompt = $"Test prompt for {generator}",
            ImageGenerator = generator,
            UploadDate = DateTime.Now,
            Filename = filename ?? $"{Guid.NewGuid():N}.png",
            Like = likes,
        };
        db.AIImages.Add(image);
        await db.SaveChangesAsync();
        return image.Id;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(DatabasePath)!, Path.GetFileName(DatabasePath) + "*"))
        {
            TryDelete(() => File.Delete(file));
        }

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
